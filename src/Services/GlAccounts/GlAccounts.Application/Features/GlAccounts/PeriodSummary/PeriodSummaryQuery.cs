namespace GlAccounts.Application.Features.GlAccounts.PeriodSummary;

public sealed record PeriodSummaryQuery(
    string AccountId,
    int? FiscalYear,
    int? FromPeriod,
    int? ToPeriod);

public sealed record PeriodBalanceItem(
    string BalanceId,
    string AccountId,
    int FiscalYear,
    int FiscalPeriod,
    string? BudgetVersion,
    decimal Balance);

public sealed record PeriodSummaryResult(
    string AccountId,
    IReadOnlyList<PeriodBalanceItem> Items,
    int Total);
