namespace GlAccounts.Application.Features.GlAccounts.ListAccountGroups;

public sealed record ListAccountGroupsQuery(string AccountId);

public sealed record AccountGroupItem(
    string GroupId,
    string GroupName,
    string? Description);

public sealed record ListAccountGroupsResult(
    string AccountId,
    IReadOnlyList<AccountGroupItem> Items,
    int Total);
