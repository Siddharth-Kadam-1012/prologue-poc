namespace GlAccounts.Api.Features.GlAccounts.StubData;

public record GroupRef(string GroupId, string Name);

public record AccountGroup(string GroupId, string Name, IReadOnlyList<GroupRef> ParentChain);

public interface IAccountGroupStore
{
    Task<IReadOnlyList<AccountGroup>> GetGroupsForAccountAsync(string accountId, CancellationToken ct = default);
    Task<IReadOnlyList<AccountGroup>> GetAllGroupsAsync(CancellationToken ct = default);
}
