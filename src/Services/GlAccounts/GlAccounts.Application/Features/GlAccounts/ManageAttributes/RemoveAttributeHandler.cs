using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.ManageAttributes;

public sealed class RemoveAttributeHandler
{
    private readonly IAccountStore _accounts;
    private readonly IAccountAttributeStore _attributes;
    private readonly IAuditStore _audit;

    public RemoveAttributeHandler(
        IAccountStore accounts,
        IAccountAttributeStore attributes,
        IAuditStore audit)
    {
        _accounts   = accounts;
        _attributes = attributes;
        _audit      = audit;
    }

    /// <summary>
    /// Returns true when deleted, false if account or attribute not found.
    /// </summary>
    public async Task<bool?> HandleAsync(RemoveAttributeCommand command, CancellationToken ct = default)
    {
        if (!await _accounts.ExistsAsync(command.AccountId, ct)) return null; // account not found

        var existing = (await _attributes.GetByAccountIdAsync(command.AccountId, ct)).ToList();
        var target = existing.FirstOrDefault(a =>
            string.Equals(a.AttributeId, command.AttributeId, StringComparison.OrdinalIgnoreCase));
        if (target is null) return null; // attribute not found

        var newList = existing
            .Where(a => !string.Equals(a.AttributeId, command.AttributeId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        await _attributes.ReplaceAllAsync(command.AccountId, newList, ct);

        await _audit.AppendAsync(new AuditEntry
        {
            EntryId    = Guid.NewGuid().ToString(),
            EntityType = "AccountAttribute",
            EntityId   = target.AttributeId,
            UserId     = command.UserId,
            Timestamp  = DateTimeOffset.UtcNow,
            ChangeType = ChangeType.Deleted,
            BeforeValues = new Dictionary<string, string?>
            {
                ["attributeTypeId"]  = target.AttributeTypeId,
                ["attributeValueId"] = target.AttributeValueId
            }
        }, ct);

        return true;
    }
}
