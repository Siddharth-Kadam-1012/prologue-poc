using GlAccounts.Api.Features.GlAccounts.DeleteAccount;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.UnitTests;

public class DeleteAccountTests
{
    private readonly InMemoryAccountStore _accountStore;
    private readonly InMemoryAccountDependencyStore _dependencyStore;
    private readonly DeleteAccountHandler _handler;

    public DeleteAccountTests()
    {
        _accountStore = new InMemoryAccountStore();
        _dependencyStore = new InMemoryAccountDependencyStore();
        _handler = new DeleteAccountHandler(_accountStore, _dependencyStore);
    }

    [Fact]
    public async Task AC_GLA_041_DeleteAccount_NoDependencies_Succeeds()
    {
        // REQ-GLA-024, AC-GLA-041
        var result = await _handler.HandleAsync("1000-100-03");

        Assert.IsType<DeleteAccountResult.Success>(result);

        // Verify account is removed from store
        var stored = await _accountStore.GetByIdAsync("1000-100-03");
        Assert.Null(stored);
    }

    [Fact]
    public async Task AC_GLA_038_NFR_GLA_005_DeleteAccount_BlockedByPostedTransactions_ReturnsBlocked()
    {
        // REQ-GLA-024, AC-GLA-038, NFR-GLA-005
        var start = DateTime.UtcNow;
        var result = await _handler.HandleAsync("1000-200-01");
        var elapsed = DateTime.UtcNow - start;

        Assert.True(elapsed.TotalSeconds < 1.0, "NFR-GLA-005: dependency check <= 1 s");

        var blocked = Assert.IsType<DeleteAccountResult.Blocked>(result);
        Assert.Contains("10001 posted transaction(s)", blocked.Detail);
        Assert.Contains(blocked.Blockers, b => b.Kind == "PostedTransactions" && b.Count == 10001);
        Assert.Contains("Inactive", blocked.Suggestion);

        // Account still exists
        var stored = await _accountStore.GetByIdAsync("1000-200-01");
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task AC_GLA_039_DeleteAccount_BlockedByUnpostedTransactions_ReturnsBlocked()
    {
        // REQ-GLA-024, AC-GLA-039
        var result = await _handler.HandleAsync("1000-300-01");

        var blocked = Assert.IsType<DeleteAccountResult.Blocked>(result);
        Assert.Contains("5 unposted transaction(s)", blocked.Detail);
        Assert.Contains(blocked.Blockers, b => b.Kind == "UnpostedTransactions" && b.Count == 5);
    }

    [Fact]
    public async Task AC_GLA_040_DeleteAccount_BlockedByBudget_ReturnsBlocked()
    {
        // REQ-GLA-024, AC-GLA-040
        var result = await _handler.HandleAsync("2000-100-01");

        var blocked = Assert.IsType<DeleteAccountResult.Blocked>(result);
        Assert.Contains(blocked.Blockers, b => b.Kind == "Budgets" && b.Count == 2);
    }

    [Fact]
    public async Task AC_GLA_040_DeleteAccount_BlockedByAllocation_ReturnsBlocked()
    {
        // REQ-GLA-024, AC-GLA-040
        var result = await _handler.HandleAsync("2000-200-01");

        var blocked = Assert.IsType<DeleteAccountResult.Blocked>(result);
        Assert.Contains(blocked.Blockers, b => b.Kind == "Allocations" && b.Count == 1);
    }

    [Fact]
    public async Task AC_GLA_040_DeleteAccount_BlockedByGroupMembership_ReturnsBlocked()
    {
        // REQ-GLA-024, AC-GLA-040
        var result = await _handler.HandleAsync("2000-300-01");

        var blocked = Assert.IsType<DeleteAccountResult.Blocked>(result);
        Assert.Contains(blocked.Blockers, b => b.Kind == "GroupMemberships" && b.Count == 1);
    }

    [Fact]
    public async Task AC_GLA_042_DeleteAccount_MultipleBlockerTypes_ListsAllBlockers()
    {
        // REQ-GLA-024, REQ-GLA-026, AC-GLA-042
        var result = await _handler.HandleAsync("3000-100-01");

        var blocked = Assert.IsType<DeleteAccountResult.Blocked>(result);
        Assert.Equal(3, blocked.Blockers.Count);
        Assert.Contains(blocked.Blockers, b => b.Kind == "PostedTransactions" && b.Count == 10);
        Assert.Contains(blocked.Blockers, b => b.Kind == "Budgets" && b.Count == 3);
        Assert.Contains(blocked.Blockers, b => b.Kind == "GroupMemberships" && b.Count == 1);
    }

    [Fact]
    public async Task DeleteAccount_NonExistent_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync("9999-999-99");
        var notFound = Assert.IsType<DeleteAccountResult.NotFound>(result);
        Assert.Equal("9999-999-99", notFound.AccountId);
    }
}
