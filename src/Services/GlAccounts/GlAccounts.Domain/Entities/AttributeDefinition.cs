namespace GlAccounts.Domain.Entities;

/// <summary>
/// An attribute type + value entry in the catalog.
/// </summary>
public sealed class AttributeValue
{
    public required string ValueId { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Attribute catalog entry — defines a valid attribute type and its allowed values.
/// </summary>
public sealed class AttributeDefinition
{
    public required string AttributeTypeId { get; init; }
    public required string AttributeTypeName { get; init; }
    public required IReadOnlyList<AttributeValue> Values { get; init; }
}
