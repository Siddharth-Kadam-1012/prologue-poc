using GlAccounts.Application.Ports;
using GlAccounts.Domain.Entities;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.DeleteAccount;

public sealed class DeleteAccountHandler
{
    private readonly IAccountStore _accounts;
    private readonly IAccountDependencyStore _dependencies;
    private readonly ITransactionStore _transactions;
    private readonly IAuditStore _audit;

    public DeleteAccountHandler(
        IAccountStore accounts,
        IAccountDependencyStore dependencies,
        ITransactionStore transactions,
        IAuditStore audit)
    {
        _accounts     = accounts;
        _dependencies = dependencies;
        _transactions = transactions;
        _audit        = audit;
    }

    /// <summary>
    /// Returns null if account not found.
    /// Throws <see cref="ConflictException"/> if dependencies block deletion.
    /// Returns true on successful deletion.
    /// </summary>
    public async Task<bool?> HandleAsync(DeleteAccountCommand command, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(command.AccountId, ct);
        if (account is null) return null;

        // Check transaction dependencies
        var txns = await _transactions.GetByAccountIdAsync(command.AccountId, ct);
        if (txns.Count > 0)
            throw new ConflictException(
                $"Account '{command.AccountId}' cannot be deleted — it has {txns.Count} transaction(s). " +
                "Consider setting the status to Inactive instead.");

        // Check other dependencies (group membership etc.)
        if (await _dependencies.HasDependenciesAsync(command.AccountId, ct))
            throw new ConflictException(
                $"Account '{command.AccountId}' cannot be deleted — it has existing dependencies. " +
                "Consider setting the status to Inactive instead.");

        await _accounts.DeleteAsync(command.AccountId, ct);

        await _audit.AppendAsync(new AuditEntry
        {
            EntryId    = Guid.NewGuid().ToString(),
            EntityType = "GLAccount",
            EntityId   = command.AccountId,
            UserId     = command.UserId,
            Timestamp  = DateTimeOffset.UtcNow,
            ChangeType = ChangeType.Deleted,
            BeforeValues = new Dictionary<string, string?>
            {
                ["accountId"]   = account.AccountId,
                ["description"] = account.Description,
                ["status"]      = account.Status.ToString()
            }
        }, ct);

        return true;
    }
}
