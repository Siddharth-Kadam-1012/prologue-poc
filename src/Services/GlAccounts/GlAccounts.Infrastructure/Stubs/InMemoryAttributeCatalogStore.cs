using System.Collections.Concurrent;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;

namespace GlAccounts.Infrastructure.Stubs;

/// <summary>
/// Thread-safe in-memory attribute catalog store (read-only for this feature; stub data phase).
/// </summary>
public sealed class InMemoryAttributeCatalogStore : IAttributeCatalogStore
{
    private readonly ConcurrentDictionary<string, AttributeDefinition> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(AttributeDefinition definition) => _store[definition.AttributeTypeId] = definition;

    public Task<AttributeDefinition?> GetByTypeIdAsync(string attributeTypeId, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(attributeTypeId, out var d) ? d : null);

    public Task<IReadOnlyList<AttributeDefinition>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AttributeDefinition>>(_store.Values.OrderBy(d => d.AttributeTypeId).ToList());

    public Task<bool> IsValidValueAsync(string attributeTypeId, string attributeValueId, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(attributeTypeId, out var def)) return Task.FromResult(false);
        var valid = def.Values.Any(v => string.Equals(v.ValueId, attributeValueId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(valid);
    }
}
