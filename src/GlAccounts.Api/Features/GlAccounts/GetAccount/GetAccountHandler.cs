using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.GetAccount;

public abstract record GetAccountResult
{
    public sealed record Success(Account Account, string ETag) : GetAccountResult;
    public sealed record NotFound(string AccountId) : GetAccountResult;
    public sealed record InvalidId(string Message) : GetAccountResult;
}

public class GetAccountHandler
{
    private readonly IAccountStore _accountStore;

    public GetAccountHandler(IAccountStore accountStore)
    {
        _accountStore = accountStore;
    }

    public async Task<GetAccountResult> HandleAsync(string? id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new GetAccountResult.InvalidId("Account ID is required.");
        }

        if (id.Length > AccountIdParser.MaxLength)
        {
            return new GetAccountResult.InvalidId($"Account ID must not exceed {AccountIdParser.MaxLength} characters.");
        }

        var account = await _accountStore.GetByIdAsync(id, ct);
        if (account is null)
        {
            return new GetAccountResult.NotFound(id);
        }

        var etag = ETagHelper.FormatETag(account.Version);
        return new GetAccountResult.Success(account, etag);
    }
}
