using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.UpdateAccount;

public sealed record UpdateAccountResult(
    string AccountId,
    string Description,
    AccountType AccountType,
    string Category,
    AccountStatus Status,
    BalanceType BalanceType,
    NormalBalance NormalBalance,
    string? CurrencyId,
    DateOnly CreatedDate,
    string CreatedBy);
