using GlAccounts.Application.Ports;

namespace GlAccounts.Application.Features.GlAccounts.PeriodSummary;

public sealed class PeriodSummaryHandler
{
    private readonly IAccountStore _accounts;
    private readonly IPeriodBalanceStore _balances;

    public PeriodSummaryHandler(IAccountStore accounts, IPeriodBalanceStore balances)
    {
        _accounts = accounts;
        _balances = balances;
    }

    /// <summary>Returns null if the account does not exist.</summary>
    public async Task<PeriodSummaryResult?> HandleAsync(PeriodSummaryQuery query, CancellationToken ct = default)
    {
        if (!await _accounts.ExistsAsync(query.AccountId, ct)) return null;

        var records = await _balances.GetByAccountIdAsync(
            query.AccountId, query.FiscalYear, query.FromPeriod, query.ToPeriod, ct);

        var items = records.Select(b => new PeriodBalanceItem(
            b.BalanceId, b.AccountId, b.FiscalYear, b.FiscalPeriod, b.BudgetVersion, b.Balance)).ToList();

        return new PeriodSummaryResult(query.AccountId, items, items.Count);
    }
}
