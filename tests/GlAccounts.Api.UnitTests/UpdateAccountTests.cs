using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;
using GlAccounts.Api.Features.GlAccounts.UpdateAccount;

namespace GlAccounts.Api.UnitTests;

public class UpdateAccountTests
{
    private readonly InMemoryAccountStore _accountStore;
    private readonly InMemoryAttributeCatalogStore _catalogStore;
    private readonly InMemoryAuditStore _auditStore;
    private readonly UpdateAccountHandler _handler;

    public UpdateAccountTests()
    {
        _accountStore = new InMemoryAccountStore();
        _catalogStore = new InMemoryAttributeCatalogStore();
        _auditStore = new InMemoryAuditStore();
        _handler = new UpdateAccountHandler(_accountStore, _catalogStore, _auditStore);
    }

    [Fact]
    public async Task AC_GLA_006_UpdateAccount_EditableFields_SucceedsWithNewETag()
    {
        // REQ-GLA-006, AC-GLA-006
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);
        var originalEtag = ETagHelper.FormatETag(account.Version);

        var request = new UpdateAccountRequest
        {
            Description = "Updated operating cash",
            Category = Category.Asset,
            CurrencyId = "EUR",
            Status = AccountStatus.Active
        };

        var result = await _handler.HandleAsync("1000-100-01", originalEtag, request, "user-finance");

        var success = Assert.IsType<UpdateAccountResult.Success>(result);
        Assert.Equal("Updated operating cash", success.Account.Description);
        Assert.Equal("EUR", success.Account.CurrencyId);
        Assert.NotEqual(originalEtag, success.ETag);

        // Verify stored
        var stored = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(stored);
        Assert.Equal("Updated operating cash", stored.Description);
        Assert.Equal("EUR", stored.CurrencyId);
    }

    [Fact]
    public async Task AC_GLA_007_UpdateAccount_CategoryAndCurrency_AreSavedAndReturned()
    {
        // REQ-GLA-006, AC-GLA-007 (stub data only): 3000-100-01 is Equity; change to Liability + GBP
        var account = await _accountStore.GetByIdAsync("3000-100-01");
        Assert.NotNull(account);

        var result = await _handler.HandleAsync(
            "3000-100-01",
            ETagHelper.FormatETag(account.Version),
            new UpdateAccountRequest
            {
                Description = account.Description,
                Category = Category.Liability,
                CurrencyId = "GBP",
                Status = account.Status
            },
            "user-finance");

        var success = Assert.IsType<UpdateAccountResult.Success>(result);
        Assert.Equal(Category.Liability, success.Account.Category);
        Assert.Equal("GBP", success.Account.CurrencyId);

        var stored = await _accountStore.GetByIdAsync("3000-100-01");
        Assert.NotNull(stored);
        Assert.Equal(Category.Liability, stored.Category);
        Assert.Equal("GBP", stored.CurrencyId);
    }

    [Fact]
    public async Task AC_GLA_009_UpdateAccount_InvalidCategoryAndCurrency_ReturnsFieldErrorsAndSavesNothing()
    {
        // REQ-GLA-007, AC-GLA-009: Statistical is not a valid category for a Financial account
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);
        var etag = ETagHelper.FormatETag(account.Version);

        var result = await _handler.HandleAsync(
            "1000-100-01",
            etag,
            new UpdateAccountRequest
            {
                Description = "Should not be saved",
                Category = Category.Statistical,
                CurrencyId = "XXX",
                Status = account.Status
            },
            "user-finance");

        var failed = Assert.IsType<UpdateAccountResult.ValidationFailed>(result);
        Assert.Contains("category", failed.Errors.Keys);
        Assert.Contains("currencyId", failed.Errors.Keys);

        var stored = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(stored);
        Assert.Equal(account.Description, stored.Description);
        Assert.Equal(account.Category, stored.Category);
        Assert.Equal(account.CurrencyId, stored.CurrencyId);
        Assert.Equal(account.Version, stored.Version);
    }

    [Fact]
    public async Task AC_GLA_010_UpdateAccount_UnknownAccount_ReturnsNotFound()
    {
        // REQ-GLA-006, AC-GLA-010
        var result = await _handler.HandleAsync(
            "9999-999-99",
            "\"any-version\"",
            new UpdateAccountRequest
            {
                Description = "Does not exist",
                Category = Category.Asset,
                CurrencyId = "USD",
                Status = AccountStatus.Active
            },
            "user-finance");

        var notFound = Assert.IsType<UpdateAccountResult.NotFound>(result);
        Assert.Equal("9999-999-99", notFound.AccountId);
    }

    [Fact]
    public async Task AC_GLA_008_AC_GLA_045_UpdateAccount_MismatchedPermanentFields_Returns400()
    {
        // REQ-GLA-004, AC-GLA-008, AC-GLA-045
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);
        var originalEtag = ETagHelper.FormatETag(account.Version);

        var request = new UpdateAccountRequest
        {
            AccountId = "9999-999-99", // Different account ID in body
            AccountType = AccountType.Statistical, // Different account type in body
            Description = "Attempted permanent change",
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active
        };

        var result = await _handler.HandleAsync("1000-100-01", originalEtag, request, "user-test");

        var failed = Assert.IsType<UpdateAccountResult.ValidationFailed>(result);
        Assert.True(failed.Errors.ContainsKey("accountId"));
        Assert.True(failed.Errors.ContainsKey("accountType"));

        // Verify nothing was changed
        var stored = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(stored);
        Assert.Equal("Cash and equivalents", stored.Description);
        Assert.Equal(AccountType.Financial, stored.AccountType);
    }

    [Fact]
    public async Task AC_GLA_022_AC_GLA_051_UpdateAccount_StatusTransitionWithJustification_AuditedCorrectly()
    {
        // REQ-GLA-014, REQ-GLA-031, AC-GLA-022, AC-GLA-051
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);
        var originalEtag = ETagHelper.FormatETag(account.Version);

        var request = new UpdateAccountRequest
        {
            Description = account.Description,
            Category = account.Category,
            CurrencyId = account.CurrencyId,
            Status = AccountStatus.Hold,
            StatusJustification = "Account put on hold for annual financial audit."
        };

        var result = await _handler.HandleAsync("1000-100-01", originalEtag, request, "auditor-01");

        var success = Assert.IsType<UpdateAccountResult.Success>(result);
        Assert.Equal(AccountStatus.Hold, success.Account.Status);
        Assert.False(success.Account.EligibleForPosting);

        // Verify audit log
        var audits = _auditStore.GetEntries("1000-100-01");
        var statusAudit = audits.FirstOrDefault(a => a.Kind == AuditEntryKind.StatusChanged);
        Assert.NotNull(statusAudit);
        Assert.Equal("Active", statusAudit.Before);
        Assert.Equal("Hold", statusAudit.After);
        Assert.Equal("Account put on hold for annual financial audit.", statusAudit.Justification);
        Assert.Equal("auditor-01", statusAudit.UserId);
    }

    [Fact]
    public async Task AC_GLA_023_UpdateAccount_StatusTransitionMissingJustification_ReturnsValidationError()
    {
        // REQ-GLA-014, AC-GLA-023
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);
        var originalEtag = ETagHelper.FormatETag(account.Version);

        var request = new UpdateAccountRequest
        {
            Description = account.Description,
            Category = account.Category,
            CurrencyId = account.CurrencyId,
            Status = AccountStatus.Inactive,
            StatusJustification = "" // Missing justification
        };

        var result = await _handler.HandleAsync("1000-100-01", originalEtag, request, "user-01");

        var failed = Assert.IsType<UpdateAccountResult.ValidationFailed>(result);
        Assert.True(failed.Errors.ContainsKey("statusJustification"));
    }

    [Fact]
    public async Task AC_GLA_023_UpdateAccount_TransitionToTemplate_ReturnsValidationError()
    {
        // REQ-GLA-014, AC-GLA-023
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);
        var originalEtag = ETagHelper.FormatETag(account.Version);

        var request = new UpdateAccountRequest
        {
            Description = account.Description,
            Category = account.Category,
            CurrencyId = account.CurrencyId,
            Status = AccountStatus.Template,
            StatusJustification = "Attempting to change to template"
        };

        var result = await _handler.HandleAsync("1000-100-01", originalEtag, request, "user-01");

        var failed = Assert.IsType<UpdateAccountResult.ValidationFailed>(result);
        Assert.True(failed.Errors.ContainsKey("status"));
        Assert.Contains(failed.Errors["status"], e => e.Contains("Template"));
    }

    [Fact]
    public async Task AC_GLA_050_UpdateAccount_FieldChanges_AuditedIndividually()
    {
        // REQ-GLA-031, AC-GLA-050
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);
        var originalEtag = ETagHelper.FormatETag(account.Version);

        var request = new UpdateAccountRequest
        {
            Description = "Brand new description",
            Category = Category.Asset,
            CurrencyId = "GBP",
            Status = AccountStatus.Active
        };

        var result = await _handler.HandleAsync("1000-100-01", originalEtag, request, "user-updater");

        Assert.IsType<UpdateAccountResult.Success>(result);

        var audits = _auditStore.GetEntries("1000-100-01");
        Assert.Contains(audits, a => a.Kind == AuditEntryKind.FieldChanged && a.Field == "description" && a.After == "Brand new description");
        Assert.Contains(audits, a => a.Kind == AuditEntryKind.FieldChanged && a.Field == "currencyId" && a.After == "GBP");
    }

    [Fact]
    public async Task AC_GLA_056_UpdateAccount_StaleETag_Returns409ConflictAndLeavesAccountUnchanged()
    {
        // REQ-GLA-035, AC-GLA-056, D-04
        var staleEtag = "\"stale-etag-token\"";

        var request = new UpdateAccountRequest
        {
            Description = "Stale attempt",
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active
        };

        var result = await _handler.HandleAsync("1000-100-01", staleEtag, request, "user-stale");

        var conflict = Assert.IsType<UpdateAccountResult.StaleVersionConflict>(result);
        Assert.Contains("changed by someone else", conflict.Message);

        // Verify account unchanged
        var stored = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(stored);
        Assert.Equal("Cash and equivalents", stored.Description);

        // Verify no audit entry
        var audits = _auditStore.GetEntries("1000-100-01");
        Assert.DoesNotContain(audits, a => a.UserId == "user-stale");
    }

    [Fact]
    public async Task UpdateAccount_MissingIfMatch_ReturnsValidationError()
    {
        var request = new UpdateAccountRequest
        {
            Description = "Missing header attempt",
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active
        };

        var result = await _handler.HandleAsync("1000-100-01", null, request, "user-test");

        var failed = Assert.IsType<UpdateAccountResult.ValidationFailed>(result);
        Assert.True(failed.Errors.ContainsKey("If-Match"));
    }
}
