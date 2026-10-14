using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.DeleteAccount;

public abstract record DeleteAccountResult
{
    public sealed record Success : DeleteAccountResult;
    public sealed record NotFound(string AccountId) : DeleteAccountResult;
    public sealed record InvalidId(string Message) : DeleteAccountResult;
    public sealed record Blocked(string Detail, List<DeleteBlocker> Blockers, string Suggestion) : DeleteAccountResult;
}

public class DeleteAccountHandler
{
    private readonly IAccountStore _accountStore;
    private readonly IAccountDependencyStore _dependencyStore;

    public DeleteAccountHandler(IAccountStore accountStore, IAccountDependencyStore dependencyStore)
    {
        _accountStore = accountStore;
        _dependencyStore = dependencyStore;
    }

    public async Task<DeleteAccountResult> HandleAsync(string? id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new DeleteAccountResult.InvalidId("Account ID is required.");
        }

        if (id.Length > AccountIdParser.MaxLength)
        {
            return new DeleteAccountResult.InvalidId($"Account ID must not exceed {AccountIdParser.MaxLength} characters.");
        }

        var account = await _accountStore.GetByIdAsync(id, ct);
        if (account is null)
        {
            return new DeleteAccountResult.NotFound(id);
        }

        // Check dependencies (REQ-GLA-024, REQ-GLA-025, REQ-GLA-026)
        var deps = await _dependencyStore.GetDependenciesAsync(id, ct);
        if (deps.HasDependencies)
        {
            var blockers = new List<DeleteBlocker>();

            if (deps.PostedTransactions > 0)
                blockers.Add(new DeleteBlocker("PostedTransactions", deps.PostedTransactions));
            if (deps.UnpostedTransactions > 0)
                blockers.Add(new DeleteBlocker("UnpostedTransactions", deps.UnpostedTransactions));
            if (deps.Budgets > 0)
                blockers.Add(new DeleteBlocker("Budgets", deps.Budgets));
            if (deps.Allocations > 0)
                blockers.Add(new DeleteBlocker("Allocations", deps.Allocations));
            if (deps.GroupMemberships > 0)
                blockers.Add(new DeleteBlocker("GroupMemberships", deps.GroupMemberships));

            string detail;
            if (deps.PostedTransactions > 0)
            {
                detail = $"Cannot delete account '{id}'. It has {deps.PostedTransactions} posted transaction(s).";
            }
            else if (deps.UnpostedTransactions > 0)
            {
                detail = $"Cannot delete account '{id}'. It has {deps.UnpostedTransactions} unposted transaction(s).";
            }
            else if (deps.Budgets > 0)
            {
                detail = $"Cannot delete account '{id}'. It is referenced by {deps.Budgets} budget record(s).";
            }
            else if (deps.Allocations > 0)
            {
                detail = $"Cannot delete account '{id}'. It is referenced by {deps.Allocations} allocation rule(s).";
            }
            else
            {
                detail = $"Cannot delete account '{id}'. It is a member of {deps.GroupMemberships} account group(s).";
            }

            const string suggestion = "Set the account status to Inactive instead of deleting it.";
            return new DeleteAccountResult.Blocked(detail, blockers, suggestion);
        }

        // Delete account
        await _accountStore.DeleteAsync(id, ct);
        return new DeleteAccountResult.Success();
    }
}
