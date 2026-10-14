using GlAccounts.Api.Domain;

namespace GlAccounts.Api.UnitTests;

public class StatusRulesTests
{
    [Theory]
    [InlineData(AccountStatus.Active, AccountStatus.Hold)]
    [InlineData(AccountStatus.Hold, AccountStatus.Inactive)]
    [InlineData(AccountStatus.Inactive, AccountStatus.Active)]
    [InlineData(AccountStatus.Active, AccountStatus.Inactive)]
    public void ValidateStatusTransition_ValidTransitionWithJustification_Succeeds(AccountStatus current, AccountStatus target)
    {
        // REQ-GLA-014, AC-GLA-022
        var (isValid, error) = StatusRules.ValidateStatusTransition(current, target, "Status updated by operations team");

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(AccountStatus.Active, AccountStatus.Hold, "")]
    [InlineData(AccountStatus.Hold, AccountStatus.Inactive, "   ")]
    [InlineData(AccountStatus.Inactive, AccountStatus.Active, null)]
    public void ValidateStatusTransition_MissingJustification_Fails(AccountStatus current, AccountStatus target, string? justification)
    {
        // REQ-GLA-014, AC-GLA-023
        var (isValid, error) = StatusRules.ValidateStatusTransition(current, target, justification);

        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("justification", error);
    }

    [Fact]
    public void ValidateStatusTransition_JustificationExceeds500Chars_Fails()
    {
        var longJustification = new string('A', 501);
        var (isValid, error) = StatusRules.ValidateStatusTransition(AccountStatus.Active, AccountStatus.Hold, longJustification);

        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("justification", error);
    }

    [Theory]
    [InlineData(AccountStatus.Active, AccountStatus.Template)]
    [InlineData(AccountStatus.Hold, AccountStatus.Template)]
    [InlineData(AccountStatus.Inactive, AccountStatus.Template)]
    [InlineData(AccountStatus.Template, AccountStatus.Active)]
    [InlineData(AccountStatus.Template, AccountStatus.Hold)]
    [InlineData(AccountStatus.Template, AccountStatus.Inactive)]
    public void ValidateStatusTransition_ToOrFromTemplate_Fails(AccountStatus current, AccountStatus target)
    {
        // REQ-GLA-014, AC-GLA-023
        var (isValid, error) = StatusRules.ValidateStatusTransition(current, target, "Trying to convert template");

        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("Template status cannot be changed", error);
    }

    [Theory]
    [InlineData(AccountStatus.Active, true)]
    [InlineData(AccountStatus.Hold, false)]
    [InlineData(AccountStatus.Inactive, false)]
    [InlineData(AccountStatus.Template, false)]
    public void EligibleForPosting_TrueOnlyForActive(AccountStatus status, bool expectedEligible)
    {
        // REQ-GLA-013, AC-GLA-020, AC-GLA-021
        var account = new Account
        {
            AccountId = "1000-100-01",
            Segments = new List<Segment> { new("Main", "1000"), new("Department", "100"), new("Sub", "01") },
            Description = "Test account",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = status,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit,
            CreatedDate = new DateOnly(2025, 1, 1),
            Version = "v1"
        };

        Assert.Equal(expectedEligible, account.EligibleForPosting);
    }
}
