using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.UpdateAccount;

public abstract record UpdateAccountResult
{
    public sealed record Success(Account Account, string ETag) : UpdateAccountResult;
    public sealed record NotFound(string AccountId) : UpdateAccountResult;
    public sealed record ValidationFailed(IDictionary<string, string[]> Errors) : UpdateAccountResult;
    public sealed record StaleVersionConflict(string Message) : UpdateAccountResult;
}

public class UpdateAccountHandler
{
    private readonly IAccountStore _accountStore;
    private readonly IAttributeCatalogStore _catalogStore;
    private readonly IAuditStore _auditStore;

    public UpdateAccountHandler(
        IAccountStore accountStore,
        IAttributeCatalogStore catalogStore,
        IAuditStore auditStore)
    {
        _accountStore = accountStore;
        _catalogStore = catalogStore;
        _auditStore = auditStore;
    }

    public async Task<UpdateAccountResult> HandleAsync(
        string id,
        string? ifMatch,
        UpdateAccountRequest request,
        string userId,
        CancellationToken ct = default)
    {
        // 1. Lookup account
        var existing = await _accountStore.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return new UpdateAccountResult.NotFound(id);
        }

        // 2. Validate request & immutability
        var validation = UpdateAccountValidator.Validate(id, ifMatch, request, existing);
        if (!validation.IsValid)
        {
            return new UpdateAccountResult.ValidationFailed(validation.Errors);
        }

        // 3. Concurrency check (REQ-GLA-035, AC-GLA-056, D-04)
        if (!ETagHelper.Matches(ifMatch, existing.Version))
        {
            return new UpdateAccountResult.StaleVersionConflict("The account was changed by someone else. Reload it and try again.");
        }

        // 4. Validate staged attribute changes
        var stagedAttributes = existing.Attributes.Select(a => new AccountAttribute(a.AttributeId, a.AttributeTypeId, a.AttributeValueId)).ToList();
        var attrAudits = new List<AuditEntry>();
        var now = DateTimeOffset.UtcNow;

        if (request.AttributeChanges != null && request.AttributeChanges.Count > 0)
        {
            var attrErrors = new Dictionary<string, string[]>();

            for (int i = 0; i < request.AttributeChanges.Count; i++)
            {
                var change = request.AttributeChanges[i];
                var action = change.Action?.Trim();

                if (string.Equals(action, "Add", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(change.AttributeTypeId) || string.IsNullOrWhiteSpace(change.AttributeValueId))
                    {
                        attrErrors[$"attributeChanges[{i}]"] = new[] { "Add requires attributeTypeId and attributeValueId." };
                        continue;
                    }

                    var isValid = await _catalogStore.IsValidAttributeAsync(change.AttributeTypeId, change.AttributeValueId, ct);
                    if (!isValid)
                    {
                        attrErrors[$"attributeChanges[{i}].attributeValueId"] = new[]
                        {
                            $"Attribute type '{change.AttributeTypeId}' and value '{change.AttributeValueId}' combination is not valid in the attribute catalog."
                        };
                        continue;
                    }

                    var newAttrId = $"att-{Guid.NewGuid():N}"[..12];
                    var newAttr = new AccountAttribute(newAttrId, change.AttributeTypeId, change.AttributeValueId);
                    stagedAttributes.Add(newAttr);

                    attrAudits.Add(new AuditEntry(
                        AuditId: Guid.NewGuid().ToString("N"),
                        AccountId: id,
                        Kind: AuditEntryKind.AttributeAdded,
                        UserId: userId,
                        Timestamp: now,
                        Field: change.AttributeTypeId,
                        After: change.AttributeValueId));
                }
                else if (string.Equals(action, "Edit", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(change.AttributeId) ||
                        string.IsNullOrWhiteSpace(change.AttributeTypeId) ||
                        string.IsNullOrWhiteSpace(change.AttributeValueId))
                    {
                        attrErrors[$"attributeChanges[{i}]"] = new[] { "Edit requires attributeId, attributeTypeId and attributeValueId." };
                        continue;
                    }

                    var existingIdx = stagedAttributes.FindIndex(a => string.Equals(a.AttributeId, change.AttributeId, StringComparison.OrdinalIgnoreCase));
                    if (existingIdx < 0)
                    {
                        attrErrors[$"attributeChanges[{i}].attributeId"] = new[] { $"Attribute '{change.AttributeId}' was not found on this account." };
                        continue;
                    }

                    var isValid = await _catalogStore.IsValidAttributeAsync(change.AttributeTypeId, change.AttributeValueId, ct);
                    if (!isValid)
                    {
                        attrErrors[$"attributeChanges[{i}].attributeValueId"] = new[]
                        {
                            $"Attribute type '{change.AttributeTypeId}' and value '{change.AttributeValueId}' combination is not valid in the attribute catalog."
                        };
                        continue;
                    }

                    var oldAttr = stagedAttributes[existingIdx];
                    var updatedAttr = new AccountAttribute(change.AttributeId, change.AttributeTypeId, change.AttributeValueId);
                    stagedAttributes[existingIdx] = updatedAttr;

                    attrAudits.Add(new AuditEntry(
                        AuditId: Guid.NewGuid().ToString("N"),
                        AccountId: id,
                        Kind: AuditEntryKind.AttributeChanged,
                        UserId: userId,
                        Timestamp: now,
                        Field: change.AttributeTypeId,
                        Before: $"{oldAttr.AttributeTypeId}:{oldAttr.AttributeValueId}",
                        After: $"{change.AttributeTypeId}:{change.AttributeValueId}"));
                }
                else if (string.Equals(action, "Remove", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(change.AttributeId))
                    {
                        attrErrors[$"attributeChanges[{i}].attributeId"] = new[] { "Remove requires attributeId." };
                        continue;
                    }

                    var existingIdx = stagedAttributes.FindIndex(a => string.Equals(a.AttributeId, change.AttributeId, StringComparison.OrdinalIgnoreCase));
                    if (existingIdx < 0)
                    {
                        attrErrors[$"attributeChanges[{i}].attributeId"] = new[] { $"Attribute '{change.AttributeId}' was not found on this account." };
                        continue;
                    }

                    var removed = stagedAttributes[existingIdx];
                    stagedAttributes.RemoveAt(existingIdx);

                    attrAudits.Add(new AuditEntry(
                        AuditId: Guid.NewGuid().ToString("N"),
                        AccountId: id,
                        Kind: AuditEntryKind.AttributeRemoved,
                        UserId: userId,
                        Timestamp: now,
                        Field: removed.AttributeTypeId,
                        Before: removed.AttributeValueId));
                }
                else
                {
                    attrErrors[$"attributeChanges[{i}].action"] = new[] { $"Invalid action '{change.Action}'. Allowed: Add, Edit, Remove." };
                }
            }

            if (attrErrors.Count > 0)
            {
                return new UpdateAccountResult.ValidationFailed(attrErrors);
            }
        }

        // 5. Track audit entries for changed account fields
        var auditEntries = new List<AuditEntry>();

        if (!string.Equals(existing.Description, request.Description, StringComparison.Ordinal))
        {
            auditEntries.Add(new AuditEntry(
                AuditId: Guid.NewGuid().ToString("N"),
                AccountId: id,
                Kind: AuditEntryKind.FieldChanged,
                UserId: userId,
                Timestamp: now,
                Field: "description",
                Before: existing.Description,
                After: request.Description));
        }

        if (existing.Category != request.Category!.Value)
        {
            auditEntries.Add(new AuditEntry(
                AuditId: Guid.NewGuid().ToString("N"),
                AccountId: id,
                Kind: AuditEntryKind.FieldChanged,
                UserId: userId,
                Timestamp: now,
                Field: "category",
                Before: existing.Category.ToString(),
                After: request.Category.Value.ToString()));
        }

        if (!string.Equals(existing.CurrencyId, request.CurrencyId, StringComparison.OrdinalIgnoreCase))
        {
            auditEntries.Add(new AuditEntry(
                AuditId: Guid.NewGuid().ToString("N"),
                AccountId: id,
                Kind: AuditEntryKind.FieldChanged,
                UserId: userId,
                Timestamp: now,
                Field: "currencyId",
                Before: existing.CurrencyId,
                After: request.CurrencyId));
        }

        if (existing.Status != request.Status!.Value)
        {
            auditEntries.Add(new AuditEntry(
                AuditId: Guid.NewGuid().ToString("N"),
                AccountId: id,
                Kind: AuditEntryKind.StatusChanged,
                UserId: userId,
                Timestamp: now,
                Before: existing.Status.ToString(),
                After: request.Status.Value.ToString(),
                Justification: request.StatusJustification));
        }

        // 6. Apply all changes and new version atomically
        var newVersion = ETagHelper.NewVersion();
        existing.Description = request.Description!;
        existing.Category = request.Category.Value;
        existing.CurrencyId = request.CurrencyId!;
        existing.Status = request.Status.Value;
        existing.Attributes = stagedAttributes;
        existing.Version = newVersion;

        await _accountStore.UpdateAsync(existing, ct);

        foreach (var audit in auditEntries)
        {
            await _auditStore.AppendAsync(audit, ct);
        }

        foreach (var audit in attrAudits)
        {
            await _auditStore.AppendAsync(audit, ct);
        }

        var etag = ETagHelper.FormatETag(newVersion);
        return new UpdateAccountResult.Success(existing, etag);
    }
}
