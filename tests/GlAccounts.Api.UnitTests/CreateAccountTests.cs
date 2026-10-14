using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.CreateAccount;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.UnitTests;

public class CreateAccountTests
{
    private readonly InMemoryAccountStore _accountStore;
    private readonly InMemoryAttributeCatalogStore _catalogStore;
    private readonly InMemoryAuditStore _auditStore;
    private readonly CreateAccountHandler _handler;

    public CreateAccountTests()
    {
        _accountStore = new InMemoryAccountStore();
        _catalogStore = new InMemoryAttributeCatalogStore();
        _auditStore = new InMemoryAuditStore();
        _handler = new CreateAccountHandler(_accountStore, _catalogStore, _auditStore);
    }

    [Fact]
    public async Task AC_GLA_001_CreateAccount_ValidFinancial_ReturnsSuccessWithETagAndSegments()
    {
        // REQ-GLA-001, REQ-GLA-002, AC-GLA-001
        var request = new CreateAccountRequest
        {
            AccountId = "1000-100-02",
            Description = "Operating cash account",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit
        };

        var result = await _handler.HandleAsync(request, "user-test-01");

        var success = Assert.IsType<CreateAccountResult.Success>(result);
        Assert.NotNull(success.Account);
        Assert.Equal("1000-100-02", success.Account.AccountId);
        Assert.Equal(3, success.Account.Segments.Count);
        Assert.Equal("1000", success.Account.Segments[0].Value);
        Assert.Equal("100", success.Account.Segments[1].Value);
        Assert.Equal("02", success.Account.Segments[2].Value);
        Assert.True(success.Account.EligibleForPosting);
        Assert.StartsWith("\"", success.ETag);
        Assert.EndsWith("\"", success.ETag);

        // Verify stored in account store
        var stored = await _accountStore.GetByIdAsync("1000-100-02");
        Assert.NotNull(stored);
        Assert.Equal("Operating cash account", stored.Description);
    }

    [Fact]
    public async Task AC_GLA_002_CreateAccount_InvalidSegmentValue_ReturnsValidationError()
    {
        // REQ-GLA-002, AC-GLA-002
        var request = new CreateAccountRequest
        {
            AccountId = "1000-777-01", // 777 is not in Department value list
            Description = "Invalid dept account",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit
        };

        var result = await _handler.HandleAsync(request, "user-test-01");

        var failed = Assert.IsType<CreateAccountResult.ValidationFailed>(result);
        Assert.True(failed.Errors.ContainsKey("accountId"));
        Assert.Contains(failed.Errors["accountId"], e => e.Contains("Department '777' is not an allowed value"));
    }

    [Fact]
    public async Task AC_GLA_003_CreateAccount_DuplicateId_ReturnsDuplicateResult()
    {
        // REQ-GLA-003, AC-GLA-003, D-02
        var request = new CreateAccountRequest
        {
            AccountId = "1000-100-01", // Seeded account already exists
            Description = "Duplicate cash account",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit
        };

        var result = await _handler.HandleAsync(request, "user-test-01");

        var dup = Assert.IsType<CreateAccountResult.Duplicate>(result);
        Assert.Equal("1000-100-01", dup.AccountId);
    }

    [Fact]
    public async Task AC_GLA_011_CreateAccount_FinancialWithStatisticalCategory_ReturnsValidationError()
    {
        // REQ-GLA-008, AC-GLA-011
        var request = new CreateAccountRequest
        {
            AccountId = "1000-100-02",
            Description = "Invalid classification",
            AccountType = AccountType.Financial,
            Category = Category.Statistical,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit
        };

        var result = await _handler.HandleAsync(request, "user-test-01");

        var failed = Assert.IsType<CreateAccountResult.ValidationFailed>(result);
        Assert.True(failed.Errors.ContainsKey("category"));
    }

    [Fact]
    public async Task AC_GLA_012_CreateAccount_FinancialMissingBalanceType_ReturnsValidationError()
    {
        // REQ-GLA-008, AC-GLA-012
        var request = new CreateAccountRequest
        {
            AccountId = "1000-100-02",
            Description = "Missing balance type",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = null,
            NormalBalance = NormalBalance.Debit
        };

        var result = await _handler.HandleAsync(request, "user-test-01");

        var failed = Assert.IsType<CreateAccountResult.ValidationFailed>(result);
        Assert.True(failed.Errors.ContainsKey("balanceType"));
    }

    [Fact]
    public async Task AC_GLA_013_AC_GLA_057_CreateAccount_StatisticalValid_Succeeds()
    {
        // REQ-GLA-008, AC-GLA-013, AC-GLA-057
        var request = new CreateAccountRequest
        {
            AccountId = "7000-100-02",
            Description = "Square footage metric",
            AccountType = AccountType.Statistical,
            Category = Category.Statistical,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = null,
            NormalBalance = null
        };

        var result = await _handler.HandleAsync(request, "user-test-01");

        var success = Assert.IsType<CreateAccountResult.Success>(result);
        Assert.Equal(AccountType.Statistical, success.Account.AccountType);
        Assert.Equal(Category.Statistical, success.Account.Category);
        Assert.Null(success.Account.BalanceType);
        Assert.Null(success.Account.NormalBalance);
        Assert.True(success.Account.EligibleForPosting);
    }

    [Fact]
    public async Task AC_GLA_043_AC_GLA_044_CreateAccount_MissingFields_ReturnsFieldLevelValidationErrors()
    {
        // REQ-GLA-027, AC-GLA-043, AC-GLA-044
        var request = new CreateAccountRequest();

        var result = await _handler.HandleAsync(request, "user-test-01");

        var failed = Assert.IsType<CreateAccountResult.ValidationFailed>(result);
        Assert.True(failed.Errors.ContainsKey("accountId"));
        Assert.True(failed.Errors.ContainsKey("description"));
        Assert.True(failed.Errors.ContainsKey("accountType"));
        Assert.True(failed.Errors.ContainsKey("category"));
        Assert.True(failed.Errors.ContainsKey("currencyId"));
        Assert.True(failed.Errors.ContainsKey("status"));
    }

    [Fact]
    public async Task AC_GLA_049_CreateAccount_WritesAuditEntryWithUserId()
    {
        // REQ-GLA-030, AC-GLA-049
        var request = new CreateAccountRequest
        {
            AccountId = "1000-100-02",
            Description = "Audited account",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit
        };

        var result = await _handler.HandleAsync(request, "auditor-42");

        Assert.IsType<CreateAccountResult.Success>(result);

        var auditEntries = _auditStore.GetEntries("1000-100-02");
        Assert.Single(auditEntries);
        var entry = auditEntries[0];
        Assert.Equal("1000-100-02", entry.AccountId);
        Assert.Equal(AuditEntryKind.Created, entry.Kind);
        Assert.Equal("auditor-42", entry.UserId);
        Assert.True(entry.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task AC_GLA_052_CreateAccount_StatusTemplate_CreatesTemplateAccount()
    {
        // REQ-GLA-032, AC-GLA-052
        var request = new CreateAccountRequest
        {
            AccountId = "9000-100-02",
            Description = "Departmental budget template",
            AccountType = AccountType.Financial,
            Category = Category.Expense,
            CurrencyId = "USD",
            Status = AccountStatus.Template,
            BalanceType = BalanceType.Periodic,
            NormalBalance = NormalBalance.Debit
        };

        var result = await _handler.HandleAsync(request, "user-admin");

        var success = Assert.IsType<CreateAccountResult.Success>(result);
        Assert.Equal(AccountStatus.Template, success.Account.Status);
        Assert.False(success.Account.EligibleForPosting); // Templates cannot be posted to
    }
}
