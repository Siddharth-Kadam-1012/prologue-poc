using System.Collections.Concurrent;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;

namespace GlAccounts.Infrastructure.Stubs;

/// <summary>
/// Thread-safe in-memory account attribute store (stub data phase).
/// Supports atomic replace-all per account (BR-GLA-009).
/// </summary>
public sealed class InMemoryAccountAttributeStore : IAccountAttributeStore
{
    private readonly ConcurrentDictionary<string, List<AccountAttribute>> _store
        = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(AccountAttribute attribute)
    {
        _store.AddOrUpdate(
            attribute.AccountId,
            _ => [attribute],
            (_, list) => { list.Add(attribute); return list; });
    }

    public Task<IReadOnlyList<AccountAttribute>> GetByAccountIdAsync(string accountId, CancellationToken ct = default)
    {
        var list = _store.TryGetValue(accountId, out var attrs)
            ? (IReadOnlyList<AccountAttribute>)attrs.AsReadOnly()
            : Array.Empty<AccountAttribute>();
        return Task.FromResult(list);
    }

    public Task ReplaceAllAsync(string accountId, IEnumerable<AccountAttribute> attributes, CancellationToken ct = default)
    {
        _store[accountId] = attributes.ToList();
        return Task.CompletedTask;
    }
}
