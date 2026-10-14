namespace GlAccounts.Api.Domain;

public static class ClassificationRules
{
    public static readonly IReadOnlyList<Category> FinancialCategories = new[]
    {
        Category.Asset,
        Category.Liability,
        Category.Equity,
        Category.Revenue,
        Category.Expense
    };

    public static readonly IReadOnlyList<Category> StatisticalCategories = new[]
    {
        Category.Statistical
    };

    public static bool IsCategoryValidForAccountType(AccountType type, Category category)
    {
        return type switch
        {
            AccountType.Financial => FinancialCategories.Contains(category),
            AccountType.Statistical => category == Category.Statistical,
            _ => false
        };
    }

    public static (bool IsValid, IReadOnlyList<string> Errors) ValidateClassification(
        AccountType accountType,
        Category category,
        BalanceType? balanceType,
        NormalBalance? normalBalance)
    {
        var errors = new List<string>();

        if (accountType == AccountType.Financial)
        {
            if (!FinancialCategories.Contains(category))
            {
                errors.Add($"Category '{category}' is invalid for Financial accounts. Allowed categories: {string.Join(", ", FinancialCategories)}.");
            }

            if (balanceType is null)
            {
                errors.Add("Balance type is required for Financial accounts.");
            }

            if (normalBalance is null)
            {
                errors.Add("Normal balance is required for Financial accounts.");
            }
        }
        else if (accountType == AccountType.Statistical)
        {
            if (category != Category.Statistical)
            {
                errors.Add($"Category '{category}' is invalid for Statistical accounts. Allowed category: Statistical.");
            }

            if (balanceType is not null)
            {
                errors.Add("Balance type must not be specified for Statistical accounts.");
            }

            if (normalBalance is not null)
            {
                errors.Add("Normal balance must not be specified for Statistical accounts.");
            }
        }

        return (errors.Count == 0, errors);
    }
}
