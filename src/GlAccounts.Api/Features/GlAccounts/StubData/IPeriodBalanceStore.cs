namespace GlAccounts.Api.Features.GlAccounts.StubData;

public record PeriodBalance(int FiscalYear, int FiscalPeriod, decimal Balance);

public record YearBalance(int FiscalYear, decimal Balance);

public record BudgetVersionAmount(string BudgetVersion, int FiscalYear, decimal Amount);

public record PeriodSummary(
    string AccountId,
    IReadOnlyList<PeriodBalance> Periods,
    IReadOnlyList<YearBalance> Years,
    IReadOnlyList<BudgetVersionAmount> BudgetVersions);

public interface IPeriodBalanceStore
{
    Task<PeriodSummary?> GetPeriodSummaryAsync(string accountId, CancellationToken ct = default);
    Task<IReadOnlyList<PeriodBalance>> GetFilteredBalancesAsync(
        string accountId,
        int? fiscalYear,
        string? sortBy,
        string? sortDirection,
        CancellationToken ct = default);
}
