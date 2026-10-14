using GlAccounts.Api.Domain;

namespace GlAccounts.Api.Features.GlAccounts.StubData;

public interface IAccountStore
{
    Task<Account?> GetByIdAsync(string accountId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string accountId, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> SearchAsync(string? query, AccountStatus? status, AccountType? accountType, Category? category, CancellationToken ct = default);
    Task CreateAsync(Account account, CancellationToken ct = default);
    Task UpdateAsync(Account account, CancellationToken ct = default);
    Task<bool> DeleteAsync(string accountId, CancellationToken ct = default);
    void Reset();
}
