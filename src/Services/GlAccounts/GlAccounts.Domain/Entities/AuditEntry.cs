using GlAccounts.Domain.Enums;

namespace GlAccounts.Domain.Entities;

/// <summary>
/// Immutable audit trail entry — append-only.
/// </summary>
public sealed class AuditEntry
{
    public required string EntryId { get; init; }
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }
    public required string UserId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required ChangeType ChangeType { get; init; }
    public IReadOnlyDictionary<string, string?>? BeforeValues { get; init; }
    public IReadOnlyDictionary<string, string?>? AfterValues { get; init; }
}
