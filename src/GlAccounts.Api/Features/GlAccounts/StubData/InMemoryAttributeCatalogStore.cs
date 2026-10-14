namespace GlAccounts.Api.Features.GlAccounts.StubData;

public class InMemoryAttributeCatalogStore : IAttributeCatalogStore
{
    private static readonly List<AttributeType> Catalog = new()
    {
        new AttributeType("dept", "Department", new List<AttributeValue>
        {
            new("finance", "Finance", "dept"),
            new("sales", "Sales", "dept"),
            new("engineering", "Engineering", "dept")
        }),
        new AttributeType("region", "Region", new List<AttributeValue>
        {
            new("emea", "EMEA", "region"),
            new("na", "North America", "region"),
            new("apac", "APAC", "region")
        }),
        new AttributeType("project", "Project", new List<AttributeValue>
        {
            new("proj-alpha", "Alpha", "project"),
            new("proj-beta", "Beta", "project")
        })
    };

    public Task<bool> IsValidAttributeAsync(string typeId, string valueId, CancellationToken ct = default)
    {
        var type = Catalog.FirstOrDefault(t => string.Equals(t.TypeId, typeId, StringComparison.OrdinalIgnoreCase));
        if (type is null) return Task.FromResult(false);

        var valid = type.Values.Any(v => string.Equals(v.ValueId, valueId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(valid);
    }

    public Task<AttributeType?> GetTypeAsync(string typeId, CancellationToken ct = default)
    {
        var type = Catalog.FirstOrDefault(t => string.Equals(t.TypeId, typeId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(type);
    }

    public Task<IReadOnlyList<AttributeType>> GetCatalogAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<AttributeType>>(Catalog);
    }

    public Task<IReadOnlyList<AttributeValue>> GetValuesForTypeAsync(string typeId, CancellationToken ct = default)
    {
        var type = Catalog.FirstOrDefault(t => string.Equals(t.TypeId, typeId, StringComparison.OrdinalIgnoreCase));
        var values = type?.Values ?? (IReadOnlyList<AttributeValue>)Array.Empty<AttributeValue>();
        return Task.FromResult(values);
    }
}
