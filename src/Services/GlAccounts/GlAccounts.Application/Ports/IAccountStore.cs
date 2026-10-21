using GlAccounts.Domain.Entities;

namespace GlAccounts.Application.Ports;

/// <summary>
/// Port for GL account persistence.
/// </summary>
public interface IAccountStore
{
    Task<GlAccount?> GetByIdAsync(string accountId, CancellationToken ct = default);
    Task<IReadOnlyList<GlAccount>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string accountId, CancellationToken ct = default);
    Task AddAsync(GlAccount account, CancellationToken ct = default);
    Task UpdateAsync(GlAccount account, CancellationToken ct = default);
    Task DeleteAsync(string accountId, CancellationToken ct = default);
}
