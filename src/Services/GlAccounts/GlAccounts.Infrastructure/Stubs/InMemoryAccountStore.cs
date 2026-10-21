using System.Collections.Concurrent;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;

namespace GlAccounts.Infrastructure.Stubs;

/// <summary>
/// Thread-safe in-memory GL account store (stub data phase).
/// </summary>
public sealed class InMemoryAccountStore : IAccountStore
{
    private readonly ConcurrentDictionary<string, GlAccount> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(GlAccount account) => _store[account.AccountId] = account;

    public Task<GlAccount?> GetByIdAsync(string accountId, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(accountId, out var a) ? a : null);

    public Task<IReadOnlyList<GlAccount>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GlAccount>>(_store.Values.OrderBy(a => a.AccountId).ToList());

    public Task<bool> ExistsAsync(string accountId, CancellationToken ct = default) =>
        Task.FromResult(_store.ContainsKey(accountId));

    public Task AddAsync(GlAccount account, CancellationToken ct = default)
    {
        if (!_store.TryAdd(account.AccountId, account))
            throw new InvalidOperationException($"Account '{account.AccountId}' already exists.");
        return Task.CompletedTask;
    }

    public Task UpdateAsync(GlAccount account, CancellationToken ct = default)
    {
        _store[account.AccountId] = account;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string accountId, CancellationToken ct = default)
    {
        _store.TryRemove(accountId, out _);
        return Task.CompletedTask;
    }
}
