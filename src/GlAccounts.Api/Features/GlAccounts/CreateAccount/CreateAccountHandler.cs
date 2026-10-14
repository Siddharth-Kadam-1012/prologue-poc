using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.CreateAccount;

public abstract record CreateAccountResult
{
    public sealed record Success(Account Account, string ETag) : CreateAccountResult;
    public sealed record ValidationFailed(IDictionary<string, string[]> Errors) : CreateAccountResult;
    public sealed record Duplicate(string AccountId) : CreateAccountResult;
    public sealed record TemplateNotFound(string TemplateAccountId) : CreateAccountResult;
}

public class CreateAccountHandler
{
    private readonly IAccountStore _accountStore;
    private readonly IAttributeCatalogStore _catalogStore;
    private readonly IAuditStore _auditStore;

    public CreateAccountHandler(
        IAccountStore accountStore,
        IAttributeCatalogStore catalogStore,
        IAuditStore auditStore)
    {
        _accountStore = accountStore;
        _catalogStore = catalogStore;
        _auditStore = auditStore;
    }

    public async Task<CreateAccountResult> HandleAsync(
        CreateAccountRequest request,
        string userId,
        CancellationToken ct = default)
    {
        // 0. Template (REQ-GLA-033, AC-GLA-053, AC-GLA-054)
        if (!string.IsNullOrWhiteSpace(request.TemplateAccountId))
        {
            var templateId = request.TemplateAccountId;
            if (templateId.Length > AccountIdParser.MaxLength)
            {
                return new CreateAccountResult.ValidationFailed(new Dictionary<string, string[]>
                {
                    ["templateAccountId"] = new[] { $"Template account ID must not exceed {AccountIdParser.MaxLength} characters." }
                });
            }

            var template = await _accountStore.GetByIdAsync(templateId, ct);
            if (template is null)
            {
                return new CreateAccountResult.TemplateNotFound(templateId);
            }

            if (template.Status != AccountStatus.Template)
            {
                return new CreateAccountResult.ValidationFailed(new Dictionary<string, string[]>
                {
                    ["templateAccountId"] = new[] { $"Account '{templateId}' is not a template and cannot be used as a template source." }
                });
            }

            ApplyTemplate(request, template);
        }

        // 1. Validation
        var validation = CreateAccountValidator.Validate(request);
        if (!validation.IsValid || validation.Segments is null)
        {
            return new CreateAccountResult.ValidationFailed(validation.Errors);
        }

        var accountId = request.AccountId!;

        // 2. Validate attributes against catalog
        var attributes = new List<AccountAttribute>();
        if (request.Attributes != null && request.Attributes.Count > 0)
        {
            var attrErrors = new Dictionary<string, string[]>();
            for (int i = 0; i < request.Attributes.Count; i++)
            {
                var input = request.Attributes[i];
                var isValid = await _catalogStore.IsValidAttributeAsync(input.AttributeTypeId, input.AttributeValueId, ct);
                if (!isValid)
                {
                    attrErrors[$"attributes[{i}].attributeValueId"] = new[]
                    {
                        $"Attribute type '{input.AttributeTypeId}' and value '{input.AttributeValueId}' combination is not valid in the attribute catalog."
                    };
                }
                else
                {
                    var attrId = $"att-{Guid.NewGuid():N}"[..12];
                    attributes.Add(new AccountAttribute(attrId, input.AttributeTypeId, input.AttributeValueId));
                }
            }

            if (attrErrors.Count > 0)
            {
                return new CreateAccountResult.ValidationFailed(attrErrors);
            }
        }

        // 3. Uniqueness check (REQ-GLA-003, AC-GLA-003, D-02)
        if (await _accountStore.ExistsAsync(accountId, ct))
        {
            return new CreateAccountResult.Duplicate(accountId);
        }

        // 4. Create Account
        var version = ETagHelper.NewVersion();
        var account = new Account
        {
            AccountId = accountId,
            Segments = validation.Segments,
            Description = request.Description!,
            AccountType = request.AccountType!.Value,
            Category = request.Category!.Value,
            CurrencyId = request.CurrencyId!,
            Status = request.Status!.Value,
            BalanceType = request.BalanceType,
            NormalBalance = request.NormalBalance,
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Version = version,
            Attributes = attributes
        };

        await _accountStore.CreateAsync(account, ct);

        // 5. Audit trail (REQ-GLA-030, AC-GLA-049)
        var auditEntry = new AuditEntry(
            AuditId: Guid.NewGuid().ToString("N"),
            AccountId: accountId,
            Kind: AuditEntryKind.Created,
            UserId: userId,
            Timestamp: DateTimeOffset.UtcNow);

        await _auditStore.AppendAsync(auditEntry, ct);

        var etag = ETagHelper.FormatETag(version);
        return new CreateAccountResult.Success(account, etag);
    }

    // Copies template settings the request did not supply. Classification settings are only
    // copied when the request keeps the template's account type.
    private static void ApplyTemplate(CreateAccountRequest request, Account template)
    {
        request.Description ??= template.Description;
        request.CurrencyId ??= template.CurrencyId;
        request.Status ??= AccountStatus.Active;

        if (request.AccountType is null || request.AccountType == template.AccountType)
        {
            request.AccountType = template.AccountType;
            request.Category ??= template.Category;
            request.BalanceType ??= template.BalanceType;
            request.NormalBalance ??= template.NormalBalance;
        }
    }
}
