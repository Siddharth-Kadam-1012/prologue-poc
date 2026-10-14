namespace GlAccounts.Api.Features.GlAccounts.StubData;

public record AttributeValue(string ValueId, string Name, string TypeId);

public record AttributeType(string TypeId, string Name, IReadOnlyList<AttributeValue> Values);

public interface IAttributeCatalogStore
{
    Task<bool> IsValidAttributeAsync(string typeId, string valueId, CancellationToken ct = default);
    Task<AttributeType?> GetTypeAsync(string typeId, CancellationToken ct = default);
    Task<IReadOnlyList<AttributeType>> GetCatalogAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AttributeValue>> GetValuesForTypeAsync(string typeId, CancellationToken ct = default);
}
