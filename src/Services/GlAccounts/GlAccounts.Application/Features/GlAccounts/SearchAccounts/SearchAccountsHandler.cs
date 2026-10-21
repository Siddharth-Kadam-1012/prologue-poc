using GlAccounts.Application.Ports;

namespace GlAccounts.Application.Features.GlAccounts.SearchAccounts;

public sealed class SearchAccountsHandler
{
    private readonly IAccountStore _accounts;

    public SearchAccountsHandler(IAccountStore accounts) => _accounts = accounts;

    public async Task<SearchAccountsResult> HandleAsync(SearchAccountsQuery query, CancellationToken ct = default)
    {
        var all = await _accounts.ListAsync(ct);

        var filtered = query.Status.HasValue
            ? all.Where(a => a.Status == query.Status.Value).ToList()
            : all.ToList();

        var items = filtered.Select(a => new AccountSummary(
            a.AccountId, a.Description, a.AccountType, a.Category,
            a.Status, a.BalanceType, a.NormalBalance, a.CurrencyId,
            a.CreatedDate, a.CreatedBy)).ToList();

        return new SearchAccountsResult(items, items.Count);
    }
}
