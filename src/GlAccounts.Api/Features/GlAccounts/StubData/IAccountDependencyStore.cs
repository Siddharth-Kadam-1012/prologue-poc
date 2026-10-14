namespace GlAccounts.Api.Features.GlAccounts.StubData;

public record AccountDependencies(
    int PostedTransactions,
    int UnpostedTransactions,
    int Budgets,
    int Allocations,
    int GroupMemberships)
{
    public bool HasDependencies =>
        PostedTransactions > 0 ||
        UnpostedTransactions > 0 ||
        Budgets > 0 ||
        Allocations > 0 ||
        GroupMemberships > 0;
}

public interface IAccountDependencyStore
{
    Task<AccountDependencies> GetDependenciesAsync(string accountId, CancellationToken ct = default);
}
