namespace GlAccounts.Api.Features.GlAccounts.StubData;

public class InMemoryPeriodBalanceStore : IPeriodBalanceStore
{
    private static readonly Dictionary<string, PeriodSummary> Summaries = InitializeSummaries();

    public Task<PeriodSummary?> GetPeriodSummaryAsync(string accountId, CancellationToken ct = default)
    {
        if (Summaries.TryGetValue(accountId, out var summary))
        {
            return Task.FromResult<PeriodSummary?>(summary);
        }

        return Task.FromResult<PeriodSummary?>(null);
    }

    public Task<IReadOnlyList<PeriodBalance>> GetFilteredBalancesAsync(
        string accountId,
        int? fiscalYear,
        string? sortBy,
        string? sortDirection,
        CancellationToken ct = default)
    {
        if (!Summaries.TryGetValue(accountId, out var summary))
        {
            return Task.FromResult<IReadOnlyList<PeriodBalance>>(Array.Empty<PeriodBalance>());
        }

        var query = summary.Periods.AsEnumerable();

        if (fiscalYear.HasValue)
        {
            query = query.Where(p => p.FiscalYear == fiscalYear.Value);
        }

        var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        query = sortBy?.ToLowerInvariant() switch
        {
            "fiscalyear" => isDesc ? query.OrderByDescending(p => p.FiscalYear).ThenByDescending(p => p.FiscalPeriod)
                                   : query.OrderBy(p => p.FiscalYear).ThenBy(p => p.FiscalPeriod),
            "balance" => isDesc ? query.OrderByDescending(p => p.Balance)
                                : query.OrderBy(p => p.Balance),
            _ => isDesc ? query.OrderByDescending(p => p.FiscalPeriod).ThenByDescending(p => p.FiscalYear)
                        : query.OrderBy(p => p.FiscalPeriod).ThenBy(p => p.FiscalYear)
        };

        return Task.FromResult<IReadOnlyList<PeriodBalance>>(query.ToList());
    }

    private static Dictionary<string, PeriodSummary> InitializeSummaries()
    {
        var dict = new Dictionary<string, PeriodSummary>(StringComparer.OrdinalIgnoreCase);

        var periods = new List<PeriodBalance>();
        for (int p = 1; p <= 12; p++)
        {
            periods.Add(new PeriodBalance(2025, p, 10000m + (p * 500m)));
        }
        for (int p = 1; p <= 3; p++)
        {
            periods.Add(new PeriodBalance(2026, p, 16000m + (p * 800m)));
        }

        var years = new List<YearBalance>
        {
            new(2025, 159000m),
            new(2026, 52800m)
        };

        var budgetVersions = new List<BudgetVersionAmount>
        {
            new("Original", 2025, 150000m),
            new("Revised", 2025, 160000m),
            new("Original", 2026, 200000m)
        };

        dict["1000-100-01"] = new PeriodSummary("1000-100-01", periods, years, budgetVersions);

        return dict;
    }
}
