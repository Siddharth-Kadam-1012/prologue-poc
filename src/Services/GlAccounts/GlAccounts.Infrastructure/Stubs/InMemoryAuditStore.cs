using System.Collections.Concurrent;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;

namespace GlAccounts.Infrastructure.Stubs;

/// <summary>
/// Thread-safe in-memory append-only audit trail store (stub data phase).
/// </summary>
public sealed class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentQueue<AuditEntry> _log = new();

    public Task AppendAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _log.Enqueue(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEntry>> GetByEntityIdAsync(string entityId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AuditEntry>>(
            _log.Where(e => string.Equals(e.EntityId, entityId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Timestamp)
                .ToList());
}
