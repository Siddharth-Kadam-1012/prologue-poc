using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.CreateAccount;
using GlAccounts.Api.Features.GlAccounts.StubData;
using GlAccounts.Api.Features.GlAccounts.Templates;

namespace GlAccounts.Api.UnitTests;

// Stub data only: T011 Templates (REQ-GLA-033, REQ-GLA-034).
// Covers AC-GLA-053 (create from template), AC-GLA-054 (non-template source), AC-GLA-055 (search templates).
public class TemplatesTests
{
    private const string TemplateId = "9000-100-01";

    private readonly InMemoryAccountStore _accountStore = new();
    private readonly CreateAccountHandler _createHandler;
    private readonly SearchAccountsHandler _searchHandler;

    public TemplatesTests()
    {
        _createHandler = new CreateAccountHandler(_accountStore, new InMemoryAttributeCatalogStore(), new InMemoryAuditStore());
        _searchHandler = new SearchAccountsHandler(_accountStore);
    }

    [Fact]
    public async Task AC_GLA_053_CreateFromTemplate_CopiesSettingsAndLeavesTemplateUnchanged()
    {
        var before = await _accountStore.GetByIdAsync(TemplateId);

        var result = await _createHandler.HandleAsync(
            new CreateAccountRequest { AccountId = "6000-100-01", TemplateAccountId = TemplateId },
            "user-test-01");

        var success = Assert.IsType<CreateAccountResult.Success>(result);
        Assert.Equal("6000-100-01", success.Account.AccountId);
        Assert.Equal(before!.Description, success.Account.Description);
        Assert.Equal(before.AccountType, success.Account.AccountType);
        Assert.Equal(before.Category, success.Account.Category);
        Assert.Equal(before.CurrencyId, success.Account.CurrencyId);
        Assert.Equal(before.BalanceType, success.Account.BalanceType);
        Assert.Equal(before.NormalBalance, success.Account.NormalBalance);
        Assert.Equal(AccountStatus.Active, success.Account.Status);

        var after = await _accountStore.GetByIdAsync(TemplateId);
        Assert.Equal(AccountStatus.Template, after!.Status);
        Assert.Equal(before.Description, after.Description);
        Assert.Equal(before.Version, after.Version);
    }

    [Fact]
    public async Task AC_GLA_053_CreateFromTemplate_RequestValuesOverrideTemplate()
    {
        var result = await _createHandler.HandleAsync(
            new CreateAccountRequest
            {
                AccountId = "6000-100-01",
                TemplateAccountId = TemplateId,
                Description = "Custom description",
                CurrencyId = "EUR",
                Status = AccountStatus.Hold
            },
            "user-test-01");

        var success = Assert.IsType<CreateAccountResult.Success>(result);
        Assert.Equal("Custom description", success.Account.Description);
        Assert.Equal("EUR", success.Account.CurrencyId);
        Assert.Equal(AccountStatus.Hold, success.Account.Status);
    }

    [Fact]
    public async Task AC_GLA_054_CreateFromTemplate_SourceNotATemplate_ReturnsValidationError()
    {
        var result = await _createHandler.HandleAsync(
            new CreateAccountRequest { AccountId = "6000-100-01", TemplateAccountId = "1000-100-01" },
            "user-test-01");

        var failed = Assert.IsType<CreateAccountResult.ValidationFailed>(result);
        var message = Assert.Single(failed.Errors["templateAccountId"]);
        Assert.Contains("not a template", message);
        Assert.False(await _accountStore.ExistsAsync("6000-100-01"));
    }

    [Fact]
    public async Task AC_GLA_053_CreateFromTemplate_UnknownTemplate_ReturnsTemplateNotFound()
    {
        var result = await _createHandler.HandleAsync(
            new CreateAccountRequest { AccountId = "6000-100-01", TemplateAccountId = "9999-999-99" },
            "user-test-01");

        var notFound = Assert.IsType<CreateAccountResult.TemplateNotFound>(result);
        Assert.Equal("9999-999-99", notFound.TemplateAccountId);
    }

    [Fact]
    public async Task AC_GLA_053_CreateFromTemplate_InvalidNewAccountId_StillValidated()
    {
        var result = await _createHandler.HandleAsync(
            new CreateAccountRequest { AccountId = "6000-777-01", TemplateAccountId = TemplateId },
            "user-test-01");

        Assert.IsType<CreateAccountResult.ValidationFailed>(result);
    }

    [Fact]
    public async Task AC_GLA_055_Search_StatusTemplate_ReturnsOnlyTemplates()
    {
        var result = await _searchHandler.HandleAsync(null, "Template", null, null, null, null);

        var success = Assert.IsType<SearchAccountsResult.Success>(result);
        Assert.NotEmpty(success.Page.Items);
        Assert.All(success.Page.Items, i => Assert.Equal(AccountStatus.Template, i.Status));
        Assert.Contains(success.Page.Items, i => i.AccountId == TemplateId);
    }

    [Fact]
    public async Task AC_GLA_055_Search_TextIsCaseInsensitiveOnDescription()
    {
        var result = await _searchHandler.HandleAsync("STANDARD EXPENSE", "Template", null, null, null, null);

        var success = Assert.IsType<SearchAccountsResult.Success>(result);
        var item = Assert.Single(success.Page.Items);
        Assert.Equal(TemplateId, item.AccountId);
    }

    [Fact]
    public async Task AC_GLA_055_Search_NoMatch_ReturnsEmptyPage()
    {
        var result = await _searchHandler.HandleAsync("no-such-text", "Template", null, null, null, null);

        var success = Assert.IsType<SearchAccountsResult.Success>(result);
        Assert.Empty(success.Page.Items);
        Assert.Equal(0, success.Page.Total);
    }

    [Fact]
    public async Task AC_GLA_055_Search_FiltersByTypeAndCategory()
    {
        var result = await _searchHandler.HandleAsync(null, null, "Statistical", "Statistical", null, null);

        var success = Assert.IsType<SearchAccountsResult.Success>(result);
        Assert.NotEmpty(success.Page.Items);
        Assert.All(success.Page.Items, i =>
        {
            Assert.Equal(AccountType.Statistical, i.AccountType);
            Assert.Equal(Category.Statistical, i.Category);
        });
    }

    [Fact]
    public async Task AC_GLA_055_Search_PagingDefaultsAndLimits()
    {
        var defaults = Assert.IsType<SearchAccountsResult.Success>(
            await _searchHandler.HandleAsync(null, null, null, null, null, null));
        Assert.Equal(1, defaults.Page.Page);
        Assert.Equal(50, defaults.Page.PageSize);

        var paged = Assert.IsType<SearchAccountsResult.Success>(
            await _searchHandler.HandleAsync(null, null, null, null, 2, 5));
        Assert.Equal(5, paged.Page.Items.Count);
        Assert.Equal(defaults.Page.Total, paged.Page.Total);

        Assert.IsType<SearchAccountsResult.InvalidRequest>(
            await _searchHandler.HandleAsync(null, null, null, null, null, 201));
        Assert.IsType<SearchAccountsResult.InvalidRequest>(
            await _searchHandler.HandleAsync(null, null, null, null, 0, null));
    }

    [Fact]
    public async Task AC_GLA_055_Search_InvalidStatus_ReturnsInvalidRequest()
    {
        var result = await _searchHandler.HandleAsync(null, "Bogus", null, null, null, null);

        var invalid = Assert.IsType<SearchAccountsResult.InvalidRequest>(result);
        Assert.Equal("status", invalid.Field);
    }
}
