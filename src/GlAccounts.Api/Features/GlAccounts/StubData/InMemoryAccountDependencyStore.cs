namespace GlAccounts.Api.Features.GlAccounts.StubData;

public class InMemoryAccountDependencyStore : IAccountDependencyStore
{
    private static readonly Dictionary<string, AccountDependencies> PrecomputedDependencies = new(StringComparer.OrdinalIgnoreCase)
    {
        // 1000-100-01: posted: 25, unposted: 0, budgets: 1, allocations: 0, groups: 1
        ["1000-100-01"] = new AccountDependencies(
            PostedTransactions: 25,
            UnpostedTransactions: 0,
            Budgets: 1,
            Allocations: 0,
            GroupMemberships: 1),

        // 1000-100-03: all zeros (deletable)
        ["1000-100-03"] = new AccountDependencies(
            PostedTransactions: 0,
            UnpostedTransactions: 0,
            Budgets: 0,
            Allocations: 0,
            GroupMemberships: 0),

        // 1000-200-01: 10,001 posted transactions
        ["1000-200-01"] = new AccountDependencies(
            PostedTransactions: 10001,
            UnpostedTransactions: 0,
            Budgets: 0,
            Allocations: 0,
            GroupMemberships: 0),

        // 1000-300-01: 5 unposted transactions
        ["1000-300-01"] = new AccountDependencies(
            PostedTransactions: 0,
            UnpostedTransactions: 5,
            Budgets: 0,
            Allocations: 0,
            GroupMemberships: 0),

        // 2000-100-01: blocked by budget
        ["2000-100-01"] = new AccountDependencies(
            PostedTransactions: 0,
            UnpostedTransactions: 0,
            Budgets: 2,
            Allocations: 0,
            GroupMemberships: 0),

        // 2000-200-01: blocked by allocation
        ["2000-200-01"] = new AccountDependencies(
            PostedTransactions: 0,
            UnpostedTransactions: 0,
            Budgets: 0,
            Allocations: 1,
            GroupMemberships: 0),

        // 2000-300-01: blocked by group membership
        ["2000-300-01"] = new AccountDependencies(
            PostedTransactions: 0,
            UnpostedTransactions: 0,
            Budgets: 0,
            Allocations: 0,
            GroupMemberships: 1),

        // 3000-100-01: multiple blocker types
        ["3000-100-01"] = new AccountDependencies(
            PostedTransactions: 10,
            UnpostedTransactions: 0,
            Budgets: 3,
            Allocations: 0,
            GroupMemberships: 1)
    };

    public Task<AccountDependencies> GetDependenciesAsync(string accountId, CancellationToken ct = default)
    {
        if (PrecomputedDependencies.TryGetValue(accountId, out var dependencies))
        {
            return Task.FromResult(dependencies);
        }

        return Task.FromResult(new AccountDependencies(0, 0, 0, 0, 0));
    }
}
