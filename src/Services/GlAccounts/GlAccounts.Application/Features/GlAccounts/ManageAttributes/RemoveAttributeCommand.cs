namespace GlAccounts.Application.Features.GlAccounts.ManageAttributes;

public sealed record RemoveAttributeCommand(
    string AccountId,
    string AttributeId,
    string UserId);
