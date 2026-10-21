using GlAccounts.Domain.Entities;

namespace GlAccounts.Application.Ports;

/// <summary>
/// Port for the append-only audit trail store.
/// </summary>
public interface IAuditStore
{
    Task AppendAsync(AuditEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> GetByEntityIdAsync(string entityId, CancellationToken ct = default);
}
