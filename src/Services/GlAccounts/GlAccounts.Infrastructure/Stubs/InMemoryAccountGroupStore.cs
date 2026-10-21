using System.Collections.Concurrent;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;

namespace GlAccounts.Infrastructure.Stubs;

/// <summary>
/// Thread-safe in-memory account group store (read-only for this feature; stub data phase).
/// </summary>
public sealed class InMemoryAccountGroupStore : IAccountGroupStore
{
    private readonly ConcurrentDictionary<string, AccountGroup> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(AccountGroup group) => _store[group.GroupId] = group;

    public Task<IReadOnlyList<AccountGroup>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AccountGroup>>(_store.Values.OrderBy(g => g.GroupId).ToList());

    public Task<AccountGroup?> GetByIdAsync(string groupId, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(groupId, out var g) ? g : null);

    public Task<IReadOnlyList<AccountGroup>> GetGroupsForAccountAsync(string accountId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AccountGroup>>(
            _store.Values
                  .Where(g => g.AccountIds.Contains(accountId, StringComparer.OrdinalIgnoreCase))
                  .OrderBy(g => g.GroupId)
                  .ToList());
}
