using System.Collections.Concurrent;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;

namespace GlAccounts.Infrastructure.Stubs;

/// <summary>
/// Thread-safe in-memory period balance store (read-only for this feature; stub data phase).
/// </summary>
public sealed class InMemoryPeriodBalanceStore : IPeriodBalanceStore
{
    private readonly ConcurrentDictionary<string, PeriodBalance> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(PeriodBalance balance) => _store[balance.BalanceId] = balance;

    public Task<IReadOnlyList<PeriodBalance>> GetByAccountIdAsync(
        string accountId,
        int? fiscalYear = null,
        int? fromPeriod = null,
        int? toPeriod = null,
        CancellationToken ct = default)
    {
        var query = _store.Values
            .Where(b => string.Equals(b.AccountId, accountId, StringComparison.OrdinalIgnoreCase));

        if (fiscalYear.HasValue)
            query = query.Where(b => b.FiscalYear == fiscalYear.Value);
        if (fromPeriod.HasValue)
            query = query.Where(b => b.FiscalPeriod >= fromPeriod.Value);
        if (toPeriod.HasValue)
            query = query.Where(b => b.FiscalPeriod <= toPeriod.Value);

        return Task.FromResult<IReadOnlyList<PeriodBalance>>(
            query.OrderBy(b => b.FiscalYear).ThenBy(b => b.FiscalPeriod).ToList());
    }
}
