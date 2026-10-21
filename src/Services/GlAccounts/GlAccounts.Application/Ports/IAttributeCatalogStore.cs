using GlAccounts.Domain.Entities;

namespace GlAccounts.Application.Ports;

/// <summary>
/// Port for reading the attribute type/value catalog (read-only).
/// </summary>
public interface IAttributeCatalogStore
{
    Task<AttributeDefinition?> GetByTypeIdAsync(string attributeTypeId, CancellationToken ct = default);
    Task<IReadOnlyList<AttributeDefinition>> ListAsync(CancellationToken ct = default);
    Task<bool> IsValidValueAsync(string attributeTypeId, string attributeValueId, CancellationToken ct = default);
}
