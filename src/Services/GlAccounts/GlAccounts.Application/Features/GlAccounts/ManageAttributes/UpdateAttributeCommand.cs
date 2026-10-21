namespace GlAccounts.Application.Features.GlAccounts.ManageAttributes;

public sealed record UpdateAttributeCommand(
    string AccountId,
    string AttributeId,
    string AttributeValueId,
    string UserId);

public sealed record UpdateAttributeResult(
    string AttributeId,
    string AccountId,
    string AttributeTypeId,
    string AttributeTypeDescription,
    string AttributeValueId,
    string AttributeValueDescription);
