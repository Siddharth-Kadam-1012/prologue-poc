using GlAccounts.Domain.Entities;

namespace GlAccounts.Application.Ports;

/// <summary>
/// Port for account attribute persistence.
/// Attribute changes are committed atomically with the parent account save (BR-GLA-009).
/// </summary>
public interface IAccountAttributeStore
{
    Task<IReadOnlyList<AccountAttribute>> GetByAccountIdAsync(string accountId, CancellationToken ct = default);
    Task ReplaceAllAsync(string accountId, IEnumerable<AccountAttribute> attributes, CancellationToken ct = default);
}
