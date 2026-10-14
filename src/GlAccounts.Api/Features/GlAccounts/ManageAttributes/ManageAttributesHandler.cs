using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.CreateAccount;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.ManageAttributes;

public abstract record ManageAttributeResult
{
    public sealed record Created(AccountAttribute Attribute, string Location, string ETag) : ManageAttributeResult;
    public sealed record Updated(AccountAttribute Attribute, string ETag) : ManageAttributeResult;
    public sealed record Deleted(string ETag) : ManageAttributeResult;
    public sealed record AccountNotFound(string AccountId) : ManageAttributeResult;
    public sealed record AttributeNotFound(string AttributeId) : ManageAttributeResult;
    public sealed record ValidationFailed(IDictionary<string, string[]> Errors) : ManageAttributeResult;
}

public class ManageAttributesHandler
{
    private readonly IAccountStore _accountStore;
    private readonly IAttributeCatalogStore _catalogStore;
    private readonly IAuditStore _auditStore;

    public ManageAttributesHandler(
        IAccountStore accountStore,
        IAttributeCatalogStore catalogStore,
        IAuditStore auditStore)
    {
        _accountStore = accountStore;
        _catalogStore = catalogStore;
        _auditStore = auditStore;
    }

    public async Task<ManageAttributeResult> AddAttributeAsync(
        string accountId,
        AttributeInput input,
        string userId,
        CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > AccountIdParser.MaxLength)
        {
            return new ManageAttributeResult.ValidationFailed(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Invalid account ID." }
            });
        }

        if (string.IsNullOrWhiteSpace(input.AttributeTypeId))
        {
            errors["attributeTypeId"] = new List<string> { "Attribute type ID is required." };
        }

        if (string.IsNullOrWhiteSpace(input.AttributeValueId))
        {
            errors["attributeValueId"] = new List<string> { "Attribute value ID is required." };
        }

        if (errors.Count > 0)
        {
            return new ManageAttributeResult.ValidationFailed(errors.ToDictionary(k => k.Key, v => v.Value.ToArray()));
        }

        var isValidCatalogEntry = await _catalogStore.IsValidAttributeAsync(input.AttributeTypeId, input.AttributeValueId, ct);
        if (!isValidCatalogEntry)
        {
            return new ManageAttributeResult.ValidationFailed(new Dictionary<string, string[]>
            {
                ["attributeValueId"] = new[] { $"Attribute type '{input.AttributeTypeId}' and value '{input.AttributeValueId}' combination is not valid in the attribute catalog." }
            });
        }

        var account = await _accountStore.GetByIdAsync(accountId, ct);
        if (account is null)
        {
            return new ManageAttributeResult.AccountNotFound(accountId);
        }

        var attributeId = $"att-{Guid.NewGuid():N}"[..12];
        var attribute = new AccountAttribute(attributeId, input.AttributeTypeId, input.AttributeValueId);

        account.Attributes.Add(attribute);
        account.Version = ETagHelper.NewVersion();
        await _accountStore.UpdateAsync(account, ct);

        // Audit entry
        var audit = new AuditEntry(
            AuditId: Guid.NewGuid().ToString("N"),
            AccountId: accountId,
            Kind: AuditEntryKind.AttributeAdded,
            UserId: userId,
            Timestamp: DateTimeOffset.UtcNow,
            Field: input.AttributeTypeId,
            After: input.AttributeValueId);

        await _auditStore.AppendAsync(audit, ct);

        var location = $"/api/gl/accounts/{accountId}/attributes/{attributeId}";
        return new ManageAttributeResult.Created(attribute, location, ETagHelper.FormatETag(account.Version));
    }

    public async Task<ManageAttributeResult> UpdateAttributeAsync(
        string accountId,
        string attributeId,
        AttributeInput input,
        string userId,
        CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > AccountIdParser.MaxLength)
        {
            return new ManageAttributeResult.ValidationFailed(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Invalid account ID." }
            });
        }

        if (string.IsNullOrWhiteSpace(attributeId) || attributeId.Length > 64)
        {
            return new ManageAttributeResult.ValidationFailed(new Dictionary<string, string[]>
            {
                ["attributeId"] = new[] { "Invalid attribute ID." }
            });
        }

        if (string.IsNullOrWhiteSpace(input.AttributeTypeId))
        {
            errors["attributeTypeId"] = new List<string> { "Attribute type ID is required." };
        }

        if (string.IsNullOrWhiteSpace(input.AttributeValueId))
        {
            errors["attributeValueId"] = new List<string> { "Attribute value ID is required." };
        }

        if (errors.Count > 0)
        {
            return new ManageAttributeResult.ValidationFailed(errors.ToDictionary(k => k.Key, v => v.Value.ToArray()));
        }

        var isValidCatalogEntry = await _catalogStore.IsValidAttributeAsync(input.AttributeTypeId, input.AttributeValueId, ct);
        if (!isValidCatalogEntry)
        {
            return new ManageAttributeResult.ValidationFailed(new Dictionary<string, string[]>
            {
                ["attributeValueId"] = new[] { $"Attribute type '{input.AttributeTypeId}' and value '{input.AttributeValueId}' combination is not valid in the attribute catalog." }
            });
        }

        var account = await _accountStore.GetByIdAsync(accountId, ct);
        if (account is null)
        {
            return new ManageAttributeResult.AccountNotFound(accountId);
        }

        var existingAttrIndex = account.Attributes.FindIndex(a => string.Equals(a.AttributeId, attributeId, StringComparison.OrdinalIgnoreCase));
        if (existingAttrIndex < 0)
        {
            return new ManageAttributeResult.AttributeNotFound(attributeId);
        }

        var oldAttr = account.Attributes[existingAttrIndex];
        var updatedAttr = new AccountAttribute(attributeId, input.AttributeTypeId, input.AttributeValueId);
        account.Attributes[existingAttrIndex] = updatedAttr;
        account.Version = ETagHelper.NewVersion();
        await _accountStore.UpdateAsync(account, ct);

        // Audit entry
        var audit = new AuditEntry(
            AuditId: Guid.NewGuid().ToString("N"),
            AccountId: accountId,
            Kind: AuditEntryKind.AttributeChanged,
            UserId: userId,
            Timestamp: DateTimeOffset.UtcNow,
            Field: input.AttributeTypeId,
            Before: $"{oldAttr.AttributeTypeId}:{oldAttr.AttributeValueId}",
            After: $"{input.AttributeTypeId}:{input.AttributeValueId}");

        await _auditStore.AppendAsync(audit, ct);

        return new ManageAttributeResult.Updated(updatedAttr, ETagHelper.FormatETag(account.Version));
    }

    public async Task<ManageAttributeResult> DeleteAttributeAsync(
        string accountId,
        string attributeId,
        string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > AccountIdParser.MaxLength)
        {
            return new ManageAttributeResult.ValidationFailed(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Invalid account ID." }
            });
        }

        if (string.IsNullOrWhiteSpace(attributeId) || attributeId.Length > 64)
        {
            return new ManageAttributeResult.ValidationFailed(new Dictionary<string, string[]>
            {
                ["attributeId"] = new[] { "Invalid attribute ID." }
            });
        }

        var account = await _accountStore.GetByIdAsync(accountId, ct);
        if (account is null)
        {
            return new ManageAttributeResult.AccountNotFound(accountId);
        }

        var existingAttrIndex = account.Attributes.FindIndex(a => string.Equals(a.AttributeId, attributeId, StringComparison.OrdinalIgnoreCase));
        if (existingAttrIndex < 0)
        {
            return new ManageAttributeResult.AttributeNotFound(attributeId);
        }

        var removed = account.Attributes[existingAttrIndex];
        account.Attributes.RemoveAt(existingAttrIndex);
        account.Version = ETagHelper.NewVersion();
        await _accountStore.UpdateAsync(account, ct);

        // Audit entry
        var audit = new AuditEntry(
            AuditId: Guid.NewGuid().ToString("N"),
            AccountId: accountId,
            Kind: AuditEntryKind.AttributeRemoved,
            UserId: userId,
            Timestamp: DateTimeOffset.UtcNow,
            Field: removed.AttributeTypeId,
            Before: removed.AttributeValueId);

        await _auditStore.AppendAsync(audit, ct);

        return new ManageAttributeResult.Deleted(ETagHelper.FormatETag(account.Version));
    }
}
