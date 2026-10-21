using GlAccounts.Application.Ports;

namespace GlAccounts.Application.Features.GlAccounts.GetAccount;

public sealed class GetAccountHandler
{
    private readonly IAccountStore _accounts;

    public GetAccountHandler(IAccountStore accounts) => _accounts = accounts;

    /// <summary>Returns the account, or null if not found.</summary>
    public async Task<GetAccountResult?> HandleAsync(GetAccountQuery query, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(query.AccountId, ct);
        if (account is null) return null;

        return new GetAccountResult(
            account.AccountId,
            account.Description,
            account.AccountType,
            account.Category,
            account.Status,
            account.BalanceType,
            account.NormalBalance,
            account.CurrencyId,
            account.CreatedDate,
            account.CreatedBy);
    }
}
