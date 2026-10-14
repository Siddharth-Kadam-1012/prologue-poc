using System.Collections.Concurrent;

namespace GlAccounts.Api.Common;

public enum AuditEntryKind
{
    Created,
    FieldChanged,
    StatusChanged,
    AttributeAdded,
    AttributeChanged,
    AttributeRemoved
}

public record AuditEntry(
    string AuditId,
    string AccountId,
    AuditEntryKind Kind,
    string UserId,
    DateTimeOffset Timestamp,
    string? Field = null,
    string? Before = null,
    string? After = null,
    string? Justification = null);

public interface IAuditStore
{
    Task AppendAsync(AuditEntry entry, CancellationToken ct = default);
    IReadOnlyList<AuditEntry> GetEntries(string? accountId = null);
    void Clear();
}

public class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();

    public Task AppendAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _entries.Enqueue(entry);
        return Task.CompletedTask;
    }

    public IReadOnlyList<AuditEntry> GetEntries(string? accountId = null)
    {
        var list = _entries.ToList();
        if (!string.IsNullOrEmpty(accountId))
        {
            return list.Where(e => string.Equals(e.AccountId, accountId, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return list;
    }

    public void Clear()
    {
        _entries.Clear();
    }
}
