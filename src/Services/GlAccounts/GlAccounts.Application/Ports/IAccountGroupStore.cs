using GlAccounts.Domain.Entities;

namespace GlAccounts.Application.Ports;

/// <summary>
/// Port for reading account group data (read-only; managed by a separate feature).
/// </summary>
public interface IAccountGroupStore
{
    Task<IReadOnlyList<AccountGroup>> ListAsync(CancellationToken ct = default);
    Task<AccountGroup?> GetByIdAsync(string groupId, CancellationToken ct = default);
    Task<IReadOnlyList<AccountGroup>> GetGroupsForAccountAsync(string accountId, CancellationToken ct = default);
}
