using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.GetAccount;

public sealed record GetAccountQuery(string AccountId);

public sealed record GetAccountResult(
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
