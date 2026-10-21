using FluentValidation;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;
using GlAccounts.Domain.Enums;
using GlAccounts.Domain.Services;

namespace GlAccounts.Application.Features.GlAccounts.UpdateAccount;

public sealed class UpdateAccountHandler
{
    private readonly IAccountStore _accounts;
    private readonly IAuditStore _audit;
    private readonly IValidator<UpdateAccountCommand> _validator;

    public UpdateAccountHandler(
        IAccountStore accounts,
        IAuditStore audit,
        IValidator<UpdateAccountCommand> validator)
    {
        _accounts  = accounts;
        _audit     = audit;
        _validator = validator;
    }

    /// <summary>
    /// Returns the updated account, or null if not found.
    /// Throws <see cref="ValidationException"/> for permanent-field change or
    /// invalid Category/Type combination.
    /// </summary>
    public async Task<UpdateAccountResult?> HandleAsync(UpdateAccountCommand command, CancellationToken ct = default)
    {
        // Reject permanent-field change attempts first
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var account = await _accounts.GetByIdAsync(command.AccountId, ct);
        if (account is null) return null;

        // Capture before-values for audit
        var before = ToDictionary(account);

        // Apply updates — only fields supplied (not null)
        if (command.Description is not null) account.Description  = command.Description;
        if (command.Status      is not null) account.Status       = command.Status.Value;
        if (command.BalanceType is not null) account.BalanceType  = command.BalanceType.Value;
        if (command.NormalBalance is not null) account.NormalBalance = command.NormalBalance.Value;
        if (command.CurrencyId  is not null) account.CurrencyId   = command.CurrencyId == "" ? null : command.CurrencyId;

        if (command.Category is not null)
        {
            if (!ClassificationRules.IsValidCategory(account.AccountType, command.Category))
                throw new ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure("category",
                        $"Category '{command.Category}' is not valid for account type '{account.AccountType}'. " +
                        $"Allowed: {string.Join(", ", ClassificationRules.GetAllowedCategories(account.AccountType))}.")
                ]);

            account.Category = command.Category;
        }

        await _accounts.UpdateAsync(account, ct);

        await _audit.AppendAsync(new AuditEntry
        {
            EntryId      = Guid.NewGuid().ToString(),
            EntityType   = "GLAccount",
            EntityId     = account.AccountId,
            UserId       = command.UserId,
            Timestamp    = DateTimeOffset.UtcNow,
            ChangeType   = ChangeType.Updated,
            BeforeValues = before,
            AfterValues  = ToDictionary(account)
        }, ct);

        return new UpdateAccountResult(
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
        ["description"]   = a.Description,
        ["category"]      = a.Category,
        ["status"]        = a.Status.ToString(),
        ["balanceType"]   = a.BalanceType.ToString(),
        ["normalBalance"] = a.NormalBalance.ToString(),
        ["currencyId"]    = a.CurrencyId
    };
}
