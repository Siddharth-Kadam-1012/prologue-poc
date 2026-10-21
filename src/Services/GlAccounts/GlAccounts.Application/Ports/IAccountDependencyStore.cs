using GlAccounts.Domain.Entities;

namespace GlAccounts.Application.Ports;

/// <summary>
/// Port for checking whether a GL account has downstream dependencies
/// that block deletion (transactions, group membership, etc.).
/// </summary>
public interface IAccountDependencyStore
{
    /// <summary>
    /// Returns true if the account is referenced by any transactions or group memberships
    /// that prevent deletion.
    /// </summary>
    Task<bool> HasDependenciesAsync(string accountId, CancellationToken ct = default);
}
