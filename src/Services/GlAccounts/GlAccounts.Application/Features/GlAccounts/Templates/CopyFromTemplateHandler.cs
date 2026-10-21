using FluentValidation;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.Templates;

public sealed class CopyFromTemplateHandler
{
    private readonly IAccountStore _accounts;
    private readonly IAuditStore _audit;

    public CopyFromTemplateHandler(IAccountStore accounts, IAuditStore audit)
    {
        _accounts = accounts;
        _audit    = audit;
    }

    /// <summary>
    /// Returns null if the source account is not found.
    /// Throws <see cref="ValidationException"/> if the source is not a Template account
    /// or if the new account ID fails segment format validation.
    /// Throws <see cref="ConflictException"/> if the new account ID already exists.
    /// </summary>
    public async Task<CopyFromTemplateResult?> HandleAsync(CopyFromTemplateCommand command, CancellationToken ct = default)
    {
        // Validate new account ID format
        if (!System.Text.RegularExpressions.Regex.IsMatch(command.NewAccountId, @"^\d{2}-\d{3}-\d{4}$"))
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure("newAccountId",
                    "New account ID does not match segment format. Expected format: NN-NNN-NNNN")
            ]);

        // Load source
        var source = await _accounts.GetByIdAsync(command.SourceAccountId, ct);
        if (source is null) return null;

        // Source must be a Template
        if (source.Status != AccountStatus.Template)
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure("accountId",
                    $"Account '{command.SourceAccountId}' is not a Template account and cannot be used as a copy source.")
            ]);

        // New ID must be unique
        if (await _accounts.ExistsAsync(command.NewAccountId, ct))
            throw new ConflictException(
                $"Account ID '{command.NewAccountId}' already exists. Please use a unique ID.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var newAccount = new GlAccount
        {
            AccountId     = command.NewAccountId,
            Description   = command.Description,
            AccountType   = source.AccountType,
            Category      = source.Category,
            Status        = AccountStatus.Active,       // copies always start Active
            BalanceType   = source.BalanceType,
            NormalBalance = source.NormalBalance,
            CurrencyId    = source.CurrencyId,
            CreatedDate   = today,
            CreatedBy     = command.UserId
        };

        await _accounts.AddAsync(newAccount, ct);

        await _audit.AppendAsync(new AuditEntry
        {
            EntryId    = Guid.NewGuid().ToString(),
            EntityType = "GLAccount",
            EntityId   = newAccount.AccountId,
            UserId     = command.UserId,
            Timestamp  = DateTimeOffset.UtcNow,
            ChangeType = ChangeType.Created,
            AfterValues = new Dictionary<string, string?>
            {
                ["accountId"]        = newAccount.AccountId,
                ["description"]      = newAccount.Description,
                ["copiedFromSource"] = command.SourceAccountId
            }
        }, ct);

        return new CopyFromTemplateResult(
            newAccount.AccountId,
            newAccount.Description,
            newAccount.AccountType,
            newAccount.Category,
            newAccount.Status,
            newAccount.BalanceType,
            newAccount.NormalBalance,
            newAccount.CurrencyId,
            newAccount.CreatedDate,
            newAccount.CreatedBy);
    }
}
