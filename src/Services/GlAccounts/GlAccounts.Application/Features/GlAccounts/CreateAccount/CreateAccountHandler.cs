using FluentValidation;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.CreateAccount;

public sealed class CreateAccountHandler
{
    private readonly IAccountStore _accounts;
    private readonly IAuditStore _audit;
    private readonly IValidator<CreateAccountCommand> _validator;

    public CreateAccountHandler(
        IAccountStore accounts,
        IAuditStore audit,
        IValidator<CreateAccountCommand> validator)
    {
        _accounts  = accounts;
        _audit     = audit;
        _validator = validator;
    }

    /// <summary>
    /// Returns the created account result, or throws <see cref="ValidationException"/>
    /// for invalid input, or <see cref="ConflictException"/> for duplicate ID.
    /// </summary>
    public async Task<CreateAccountResult> HandleAsync(CreateAccountCommand command, CancellationToken ct = default)
    {
        // Validate (throws ValidationException on failure)
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // Duplicate check
        if (await _accounts.ExistsAsync(command.AccountId, ct))
            throw new ConflictException($"Account ID '{command.AccountId}' already exists. Please use a unique ID.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var account = new GlAccount
        {
            AccountId     = command.AccountId,
            Description   = command.Description,
            AccountType   = command.AccountType,
            Category      = command.Category,
            Status        = command.Status,
            BalanceType   = command.BalanceType,
            NormalBalance = command.NormalBalance,
            CurrencyId    = command.CurrencyId,
            CreatedDate   = today,
            CreatedBy     = command.UserId
        };

        await _accounts.AddAsync(account, ct);

        await _audit.AppendAsync(new AuditEntry
        {
            EntryId     = Guid.NewGuid().ToString(),
            EntityType  = "GLAccount",
            EntityId    = account.AccountId,
            UserId      = command.UserId,
            Timestamp   = DateTimeOffset.UtcNow,
            ChangeType  = ChangeType.Created,
            BeforeValues = null,
            AfterValues  = ToDictionary(account)
        }, ct);

        return new CreateAccountResult(
            account.AccountId,
            account.Description,
            account.AccountType,
            account.Category,
            account.Status,
            account.BalanceType,
            account.NormalBalance,
            account.CurrencyId,
            account.CreatedDate,
            account.CreatedBy);
    }

    private static Dictionary<string, string?> ToDictionary(GlAccount a) => new()
    {
        ["accountId"]     = a.AccountId,
        ["description"]   = a.Description,
        ["accountType"]   = a.AccountType.ToString(),
        ["category"]      = a.Category,
        ["status"]        = a.Status.ToString(),
        ["balanceType"]   = a.BalanceType.ToString(),
        ["normalBalance"] = a.NormalBalance.ToString(),
        ["currencyId"]    = a.CurrencyId,
        ["createdBy"]     = a.CreatedBy
    };
}
