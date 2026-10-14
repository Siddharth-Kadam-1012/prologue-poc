using GlAccounts.Api.Domain;

namespace GlAccounts.Api.UnitTests;

public class ClassificationRulesTests
{
    [Theory]
    [InlineData(Category.Asset)]
    [InlineData(Category.Liability)]
    [InlineData(Category.Equity)]
    [InlineData(Category.Revenue)]
    [InlineData(Category.Expense)]
    public void ValidateClassification_FinancialWithValidCategoryAndBalances_Succeeds(Category category)
    {
        // REQ-GLA-008, AC-GLA-011, AC-GLA-012
        var (isValid, errors) = ClassificationRules.ValidateClassification(
            AccountType.Financial,
            category,
            BalanceType.BalanceForward,
            NormalBalance.Debit);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateClassification_FinancialWithStatisticalCategory_Fails()
    {
        // REQ-GLA-008
        var (isValid, errors) = ClassificationRules.ValidateClassification(
            AccountType.Financial,
            Category.Statistical,
            BalanceType.BalanceForward,
            NormalBalance.Debit);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Category 'Statistical' is invalid for Financial accounts"));
    }

    [Fact]
    public void ValidateClassification_FinancialMissingBalanceTypeOrNormalBalance_Fails()
    {
        // REQ-GLA-008
        var (isValid1, errors1) = ClassificationRules.ValidateClassification(
            AccountType.Financial,
            Category.Asset,
            null,
            NormalBalance.Debit);

        Assert.False(isValid1);
        Assert.Contains(errors1, e => e.Contains("Balance type is required"));

        var (isValid2, errors2) = ClassificationRules.ValidateClassification(
            AccountType.Financial,
            Category.Asset,
            BalanceType.Periodic,
            null);

        Assert.False(isValid2);
        Assert.Contains(errors2, e => e.Contains("Normal balance is required"));
    }

    [Fact]
    public void ValidateClassification_StatisticalWithStatisticalCategoryAndNoBalances_Succeeds()
    {
        // REQ-GLA-008, AC-GLA-057
        var (isValid, errors) = ClassificationRules.ValidateClassification(
            AccountType.Statistical,
            Category.Statistical,
            null,
            null);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(Category.Asset)]
    [InlineData(Category.Expense)]
    public void ValidateClassification_StatisticalWithFinancialCategory_Fails(Category category)
    {
        // REQ-GLA-008, AC-GLA-057
        var (isValid, errors) = ClassificationRules.ValidateClassification(
            AccountType.Statistical,
            category,
            null,
            null);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Category") && e.Contains("Statistical"));
    }

    [Fact]
    public void ValidateClassification_StatisticalWithBalances_Fails()
    {
        // REQ-GLA-008, AC-GLA-057
        var (isValid, errors) = ClassificationRules.ValidateClassification(
            AccountType.Statistical,
            Category.Statistical,
            BalanceType.Periodic,
            NormalBalance.Debit);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Balance type must not be specified"));
        Assert.Contains(errors, e => e.Contains("Normal balance must not be specified"));
    }
}
