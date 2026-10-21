using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.SearchAccounts;

public sealed record SearchAccountsQuery(AccountStatus? Status);

public sealed record AccountSummary(
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

public sealed record SearchAccountsResult(
    IReadOnlyList<AccountSummary> Items,
    int Total);
