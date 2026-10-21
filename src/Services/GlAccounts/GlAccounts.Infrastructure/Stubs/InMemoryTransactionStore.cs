using System.Collections.Concurrent;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;

namespace GlAccounts.Infrastructure.Stubs;

/// <summary>
/// Thread-safe in-memory GL transaction store (read-only for this feature; stub data phase).
/// </summary>
public sealed class InMemoryTransactionStore : ITransactionStore
{
    private readonly ConcurrentDictionary<string, GlTransaction> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(GlTransaction transaction) => _store[transaction.TransactionId] = transaction;

    public Task<IReadOnlyList<GlTransaction>> GetByAccountIdAsync(string accountId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GlTransaction>>(
            _store.Values
                  .Where(t => string.Equals(t.AccountId, accountId, StringComparison.OrdinalIgnoreCase))
                  .OrderByDescending(t => t.PostingDate)
                  .ToList());

    public Task<bool> HasTransactionsAsync(string accountId, CancellationToken ct = default) =>
        Task.FromResult(_store.Values.Any(t => string.Equals(t.AccountId, accountId, StringComparison.OrdinalIgnoreCase)));
}
