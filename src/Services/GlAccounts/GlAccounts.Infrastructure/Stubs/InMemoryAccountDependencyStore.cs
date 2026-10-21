using GlAccounts.Application.Ports;

namespace GlAccounts.Infrastructure.Stubs;

/// <summary>
/// Checks whether a GL account has downstream dependencies blocking deletion.
/// Delegates to the transaction store (has any posted/unposted transactions).
/// </summary>
public sealed class InMemoryAccountDependencyStore : IAccountDependencyStore
{
    private readonly ITransactionStore _transactionStore;

    public InMemoryAccountDependencyStore(ITransactionStore transactionStore)
    {
        _transactionStore = transactionStore;
    }

    public Task<bool> HasDependenciesAsync(string accountId, CancellationToken ct = default) =>
        _transactionStore.HasTransactionsAsync(accountId, ct);
}
