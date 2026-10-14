namespace GlAccounts.Api.Features.GlAccounts.StubData;

public class InMemoryAccountGroupStore : IAccountGroupStore
{
    private static readonly List<AccountGroup> AllGroups = new()
    {
        new AccountGroup("grp-all", "All Accounts", Array.Empty<GroupRef>()),
        new AccountGroup("grp-revenue", "Revenue Accounts", new List<GroupRef>
        {
            new("grp-all", "All Accounts")
        }),
        new AccountGroup("grp-revenue-na", "North America Revenue", new List<GroupRef>
        {
            new("grp-revenue", "Revenue Accounts"),
            new("grp-all", "All Accounts")
        }),
        new AccountGroup("grp-expense", "Expense Accounts", new List<GroupRef>
        {
            new("grp-all", "All Accounts")
        })
    };

    private static readonly Dictionary<string, List<string>> AccountGroupMemberships = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1000-100-01"] = new() { "grp-revenue-na" },
        ["2000-300-01"] = new() { "grp-expense" },
        ["3000-100-01"] = new() { "grp-all" }
    };

    public Task<IReadOnlyList<AccountGroup>> GetGroupsForAccountAsync(string accountId, CancellationToken ct = default)
    {
        if (AccountGroupMemberships.TryGetValue(accountId, out var groupIds))
        {
            var groups = AllGroups.Where(g => groupIds.Contains(g.GroupId, StringComparer.OrdinalIgnoreCase)).ToList();
            return Task.FromResult<IReadOnlyList<AccountGroup>>(groups);
        }

        return Task.FromResult<IReadOnlyList<AccountGroup>>(Array.Empty<AccountGroup>());
    }

    public Task<IReadOnlyList<AccountGroup>> GetAllGroupsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<AccountGroup>>(AllGroups);
    }
}
