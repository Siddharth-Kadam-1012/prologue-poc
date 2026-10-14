using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.GetAccount;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.UnitTests;

public class GetAccountTests
{
    private readonly InMemoryAccountStore _accountStore;
    private readonly GetAccountHandler _handler;

    public GetAccountTests()
    {
        _accountStore = new InMemoryAccountStore();
        _handler = new GetAccountHandler(_accountStore);
    }

    [Fact]
    public async Task AC_GLA_004_GetAccount_ExistingActiveAccount_ReturnsSuccessWithETagAndAttributes()
    {
        // REQ-GLA-005, AC-GLA-004
        var result = await _handler.HandleAsync("1000-100-01");

        var success = Assert.IsType<GetAccountResult.Success>(result);
        var account = success.Account;
        Assert.Equal("1000-100-01", account.AccountId);
        Assert.Equal("Cash and equivalents", account.Description);
        Assert.Equal(AccountType.Financial, account.AccountType);
        Assert.Equal(Category.Asset, account.Category);
        Assert.Equal("USD", account.CurrencyId);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.True(account.EligibleForPosting);
        Assert.Equal(3, account.Segments.Count);
        Assert.Equal(2, account.Attributes.Count);
        Assert.StartsWith("\"", success.ETag);
        Assert.EndsWith("\"", success.ETag);
    }

    [Fact]
    public async Task AC_GLA_005_GetAccount_NonExistentAccount_ReturnsNotFound()
    {
        // REQ-GLA-005, AC-GLA-005
        var result = await _handler.HandleAsync("9999-999-99");

        var notFound = Assert.IsType<GetAccountResult.NotFound>(result);
        Assert.Equal("9999-999-99", notFound.AccountId);
    }

    [Theory]
    [InlineData("1000-100-01", AccountStatus.Active, true)]
    [InlineData("4000-100-01", AccountStatus.Hold, false)]
    [InlineData("5000-100-01", AccountStatus.Inactive, false)]
    [InlineData("9000-100-01", AccountStatus.Template, false)]
    public async Task AC_GLA_020_AC_GLA_021_GetAccount_SeededStatuses_ReflectPostingEligibility(
        string accountId,
        AccountStatus expectedStatus,
        bool expectedEligibility)
    {
        // REQ-GLA-013, AC-GLA-020, AC-GLA-021
        var result = await _handler.HandleAsync(accountId);

        var success = Assert.IsType<GetAccountResult.Success>(result);
        Assert.Equal(expectedStatus, success.Account.Status);
        Assert.Equal(expectedEligibility, success.Account.EligibleForPosting);
    }

    [Fact]
    public async Task GetAccount_IdExceeding32Chars_ReturnsInvalidId()
    {
        var longId = "1000-100-01-" + new string('9', 30);
        var result = await _handler.HandleAsync(longId);

        var invalid = Assert.IsType<GetAccountResult.InvalidId>(result);
        Assert.Contains("32 characters", invalid.Message);
    }
}
