using GlAccounts.Application.Ports;

namespace GlAccounts.Application.Features.GlAccounts.ListAccountGroups;

public sealed class ListAccountGroupsHandler
{
    private readonly IAccountStore _accounts;
    private readonly IAccountGroupStore _groups;

    public ListAccountGroupsHandler(IAccountStore accounts, IAccountGroupStore groups)
    {
        _accounts = accounts;
        _groups   = groups;
    }

    /// <summary>Returns null if the account does not exist.</summary>
    public async Task<ListAccountGroupsResult?> HandleAsync(ListAccountGroupsQuery query, CancellationToken ct = default)
    {
        if (!await _accounts.ExistsAsync(query.AccountId, ct)) return null;

        var groups = await _groups.GetGroupsForAccountAsync(query.AccountId, ct);
        var items  = groups.Select(g => new AccountGroupItem(g.GroupId, g.GroupName, g.Description)).ToList();

        return new ListAccountGroupsResult(query.AccountId, items, items.Count);
    }
}
