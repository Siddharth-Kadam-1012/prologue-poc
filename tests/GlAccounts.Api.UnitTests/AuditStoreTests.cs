using GlAccounts.Api.Common;

namespace GlAccounts.Api.UnitTests;

public class AuditStoreTests
{
    [Fact]
    public async Task NFR_GLA_007_AuditStore_IsAppendOnlyAndPreservesOrder()
    {
        // NFR-GLA-007, REQ-GLA-030, REQ-GLA-031
        var store = new InMemoryAuditStore();

        var entry1 = new AuditEntry(
            AuditId: "aud-001",
            AccountId: "1000-100-01",
            Kind: AuditEntryKind.Created,
            UserId: "user-admin",
            Timestamp: DateTimeOffset.UtcNow);

        var entry2 = new AuditEntry(
            AuditId: "aud-002",
            AccountId: "1000-100-01",
            Kind: AuditEntryKind.FieldChanged,
            UserId: "user-finance",
            Timestamp: DateTimeOffset.UtcNow.AddSeconds(1),
            Field: "description",
            Before: "Old description",
            After: "New description");

        var entry3 = new AuditEntry(
            AuditId: "aud-003",
            AccountId: "1000-100-01",
            Kind: AuditEntryKind.StatusChanged,
            UserId: "user-auditor",
            Timestamp: DateTimeOffset.UtcNow.AddSeconds(2),
            Justification: "Quarterly review hold");

        var entryOther = new AuditEntry(
            AuditId: "aud-004",
            AccountId: "2000-100-01",
            Kind: AuditEntryKind.Created,
            UserId: "unknown",
            Timestamp: DateTimeOffset.UtcNow.AddSeconds(3));

        await store.AppendAsync(entry1);
        await store.AppendAsync(entry2);
        await store.AppendAsync(entry3);
        await store.AppendAsync(entryOther);

        var allEntries = store.GetEntries();
        Assert.Equal(4, allEntries.Count);
        Assert.Equal("aud-001", allEntries[0].AuditId);
        Assert.Equal("aud-002", allEntries[1].AuditId);
        Assert.Equal("aud-003", allEntries[2].AuditId);
        Assert.Equal("aud-004", allEntries[3].AuditId);

        var accountEntries = store.GetEntries("1000-100-01");
        Assert.Equal(3, accountEntries.Count);
        Assert.All(accountEntries, e => Assert.Equal("1000-100-01", e.AccountId));
    }

    [Fact]
    public async Task NFR_GLA_007_AuditStore_SupportsAllEntryKinds()
    {
        var store = new InMemoryAuditStore();
        var kinds = Enum.GetValues<AuditEntryKind>();

        foreach (var kind in kinds)
        {
            await store.AppendAsync(new AuditEntry(
                AuditId: Guid.NewGuid().ToString(),
                AccountId: "1000-100-01",
                Kind: kind,
                UserId: "test-user",
                Timestamp: DateTimeOffset.UtcNow));
        }

        var entries = store.GetEntries("1000-100-01");
        Assert.Equal(kinds.Length, entries.Count);
        foreach (var kind in kinds)
        {
            Assert.Contains(entries, e => e.Kind == kind);
        }
    }
}
