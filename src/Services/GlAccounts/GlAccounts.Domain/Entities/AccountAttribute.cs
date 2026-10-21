namespace GlAccounts.Domain.Entities;

/// <summary>
/// Links a GL account to a reporting classification (Attribute Type + Value).
/// </summary>
public sealed class AccountAttribute
{
    public required string AttributeId { get; init; }
    public required string AccountId { get; init; }
    public required string AttributeTypeId { get; init; }
    public required string AttributeTypeDescription { get; init; }
    public required string AttributeValueId { get; init; }
    public required string AttributeValueDescription { get; init; }
}
