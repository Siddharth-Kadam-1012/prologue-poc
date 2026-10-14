using System.Text;
using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.ListAccountGroups;

public abstract record ListAccountGroupsResult
{
    public sealed record Success(AccountGroupListResponse Response) : ListAccountGroupsResult;
    public sealed record ExportSuccess(byte[] Content, string FileName) : ListAccountGroupsResult;
    public sealed record NotFound(string AccountId) : ListAccountGroupsResult;
    public sealed record InvalidId(string Message) : ListAccountGroupsResult;
}

public class ListAccountGroupsHandler
{
    private readonly IAccountStore _accountStore;
    private readonly IAccountGroupStore _groupStore;

    public ListAccountGroupsHandler(IAccountStore accountStore, IAccountGroupStore groupStore)
    {
        _accountStore = accountStore;
        _groupStore = groupStore;
    }

    public async Task<ListAccountGroupsResult> GetGroupsAsync(string? id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > AccountIdParser.MaxLength)
        {
            return new ListAccountGroupsResult.InvalidId($"Account ID must be between 1 and {AccountIdParser.MaxLength} characters.");
        }

        var account = await _accountStore.GetByIdAsync(id, ct);
        if (account is null)
        {
            return new ListAccountGroupsResult.NotFound(id);
        }

        var groups = await _groupStore.GetGroupsForAccountAsync(id, ct);
        return new ListAccountGroupsResult.Success(new AccountGroupListResponse { Items = groups });
    }

    public async Task<ListAccountGroupsResult> ExportGroupsAsync(string? id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > AccountIdParser.MaxLength)
        {
            return new ListAccountGroupsResult.InvalidId($"Account ID must be between 1 and {AccountIdParser.MaxLength} characters.");
        }

        var account = await _accountStore.GetByIdAsync(id, ct);
        if (account is null)
        {
            return new ListAccountGroupsResult.NotFound(id);
        }

        var groups = await _groupStore.GetGroupsForAccountAsync(id, ct);

        var sb = new StringBuilder();
        sb.AppendLine("groupId,name,parentChain");

        foreach (var group in groups)
        {
            var chainStr = group.ParentChain.Count > 0
                ? string.Join(" > ", group.ParentChain.Select(p => p.Name))
                : string.Empty;

            var escapedName = EscapeCsv(group.Name);
            var escapedChain = EscapeCsv(chainStr);

            sb.AppendLine($"{group.GroupId},{escapedName},{escapedChain}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"account-groups-{id}.csv";

        return new ListAccountGroupsResult.ExportSuccess(bytes, fileName);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
