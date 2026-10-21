using FluentValidation;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.ManageAttributes;

public sealed class UpdateAttributeHandler
{
    private readonly IAccountStore _accounts;
    private readonly IAccountAttributeStore _attributes;
    private readonly IAttributeCatalogStore _catalog;
    private readonly IAuditStore _audit;

    public UpdateAttributeHandler(
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

    /// <summary>
    /// Returns the updated attribute, null if account or attribute not found,
    /// or throws ValidationException if new value is invalid.
    /// </summary>
    public async Task<UpdateAttributeResult?> HandleAsync(UpdateAttributeCommand command, CancellationToken ct = default)
    {
        if (!await _accounts.ExistsAsync(command.AccountId, ct)) return null;

        var existing = (await _attributes.GetByAccountIdAsync(command.AccountId, ct)).ToList();
        var target = existing.FirstOrDefault(a =>
            string.Equals(a.AttributeId, command.AttributeId, StringComparison.OrdinalIgnoreCase));
        if (target is null) return null;

        var definition = await _catalog.GetByTypeIdAsync(target.AttributeTypeId, ct);
        if (definition is null)
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure("attributeTypeId",
                    $"Attribute type '{target.AttributeTypeId}' no longer exists in the catalog.")
            ]);

        var value = definition.Values
            .FirstOrDefault(v => string.Equals(v.ValueId, command.AttributeValueId, StringComparison.OrdinalIgnoreCase));
        if (value is null)
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure("attributeValueId",
                    $"Attribute value '{command.AttributeValueId}' is not valid for type '{target.AttributeTypeId}'.")
            ]);

        var updated = new AccountAttribute
        {
            AttributeId               = target.AttributeId,
            AccountId                 = target.AccountId,
            AttributeTypeId           = target.AttributeTypeId,
            AttributeTypeDescription  = target.AttributeTypeDescription,
            AttributeValueId          = value.ValueId,
            AttributeValueDescription = value.Description
        };

        var newList = existing
            .Select(a => string.Equals(a.AttributeId, command.AttributeId, StringComparison.OrdinalIgnoreCase) ? updated : a)
            .ToList();

        await _attributes.ReplaceAllAsync(command.AccountId, newList, ct);

        await _audit.AppendAsync(new AuditEntry
        {
            EntryId    = Guid.NewGuid().ToString(),
            EntityType = "AccountAttribute",
            EntityId   = updated.AttributeId,
            UserId     = command.UserId,
            Timestamp  = DateTimeOffset.UtcNow,
            ChangeType = ChangeType.Updated,
            BeforeValues = new Dictionary<string, string?> { ["attributeValueId"] = target.AttributeValueId },
            AfterValues  = new Dictionary<string, string?> { ["attributeValueId"] = updated.AttributeValueId }
        }, ct);

        return new UpdateAttributeResult(
            updated.AttributeId,
            updated.AccountId,
            updated.AttributeTypeId,
            updated.AttributeTypeDescription,
            updated.AttributeValueId,
            updated.AttributeValueDescription);
    }
}
