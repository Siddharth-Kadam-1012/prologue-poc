namespace GlAccounts.Application.Features.GlAccounts.ManageAttributes;

public sealed record AddAttributeCommand(
    string AccountId,
    string AttributeTypeId,
    string AttributeValueId,
    string UserId);

public sealed record AddAttributeResult(
    string AttributeId,
    string AccountId,
    string AttributeTypeId,
    string AttributeTypeDescription,
    string AttributeValueId,
    string AttributeValueDescription);
