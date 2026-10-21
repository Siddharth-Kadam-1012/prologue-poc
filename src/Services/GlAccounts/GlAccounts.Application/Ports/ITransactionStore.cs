using GlAccounts.Domain.Entities;

namespace GlAccounts.Application.Ports;

/// <summary>
/// Port for reading GL journal transactions (read-only; written by the journal module).
/// </summary>
public interface ITransactionStore
{
    Task<IReadOnlyList<GlTransaction>> GetByAccountIdAsync(string accountId, CancellationToken ct = default);
    Task<bool> HasTransactionsAsync(string accountId, CancellationToken ct = default);
}
