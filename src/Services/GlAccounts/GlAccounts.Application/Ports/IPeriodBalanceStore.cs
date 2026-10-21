using GlAccounts.Domain.Entities;

namespace GlAccounts.Application.Ports;

/// <summary>
/// Port for reading period balance snapshots (read-only; written by the journal module).
/// </summary>
public interface IPeriodBalanceStore
{
    Task<IReadOnlyList<PeriodBalance>> GetByAccountIdAsync(
        string accountId,
        int? fiscalYear = null,
        int? fromPeriod = null,
        int? toPeriod = null,
        CancellationToken ct = default);
}
