using FluentValidation;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.ManageAttributes;

public sealed class AddAttributeHandler
{
    private readonly IAccountStore _accounts;
    private readonly IAccountAttributeStore _attributes;
    private readonly IAttributeCatalogStore _catalog;
    private readonly IAuditStore _audit;

    public AddAttributeHandler(
        IAccountStore accounts,
        IAccountAttributeStore attributes,
        IAttributeCatalogStore catalog,
        IAuditStore audit)
    {
        _accounts   = accounts;
        _attributes = attributes;
        _catalog    = catalog;
        _audit      = audit;
    }

    /// <summary>Returns the new attribute, null if account not found, or throws ValidationException.</summary>
    public async Task<AddAttributeResult?> HandleAsync(AddAttributeCommand command, CancellationToken ct = default)
    {
        if (!await _accounts.ExistsAsync(command.AccountId, ct)) return null;

        var definition = await _catalog.GetByTypeIdAsync(command.AttributeTypeId, ct);
        if (definition is null)
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure("attributeTypeId",
                    $"Attribute type '{command.AttributeTypeId}' does not exist in the catalog.")
            ]);

        var value = definition.Values
            .FirstOrDefault(v => string.Equals(v.ValueId, command.AttributeValueId, StringComparison.OrdinalIgnoreCase));
        if (value is null)
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure("attributeValueId",
                    $"Attribute value '{command.AttributeValueId}' is not valid for type '{command.AttributeTypeId}'.")
            ]);

        var existing = await _attributes.GetByAccountIdAsync(command.AccountId, ct);
        var newAttributeId = Guid.NewGuid().ToString();

        var newAttribute = new AccountAttribute
        {
            AttributeId               = newAttributeId,
            AccountId                 = command.AccountId,
            AttributeTypeId           = definition.AttributeTypeId,
            AttributeTypeDescription  = definition.AttributeTypeName,
            AttributeValueId          = value.ValueId,
            AttributeValueDescription = value.Description
        };

        // Atomic: replace all attributes for this account
        var updated = existing.Append(newAttribute).ToList();
        await _attributes.ReplaceAllAsync(command.AccountId, updated, ct);

        await _audit.AppendAsync(new AuditEntry
        {
            EntryId    = Guid.NewGuid().ToString(),
            EntityType = "AccountAttribute",
            EntityId   = newAttributeId,
            UserId     = command.UserId,
            Timestamp  = DateTimeOffset.UtcNow,
            ChangeType = ChangeType.Created,
            AfterValues = new Dictionary<string, string?>
            {
                ["attributeTypeId"]  = newAttribute.AttributeTypeId,
                ["attributeValueId"] = newAttribute.AttributeValueId
            }
        }, ct);

        return new AddAttributeResult(
            newAttribute.AttributeId,
            newAttribute.AccountId,
            newAttribute.AttributeTypeId,
            newAttribute.AttributeTypeDescription,
            newAttribute.AttributeValueId,
            newAttribute.AttributeValueDescription);
    }
}
