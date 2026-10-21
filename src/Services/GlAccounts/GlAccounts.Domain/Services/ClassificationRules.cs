using GlAccounts.Domain.Enums;

namespace GlAccounts.Domain.Services;

/// <summary>
/// Defines which Category values are valid for each AccountType (BR-GLA-007).
/// </summary>
public static class ClassificationRules
{
    private static readonly IReadOnlySet<string> FinancialCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Asset",
        "Liability",
        "Equity",
        "Revenue",
        "Expense"
    };

    private static readonly IReadOnlySet<string> StatisticalCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Headcount",
        "Units",
        "Area",
        "Other"
    };

    /// <summary>
    /// Returns true if the category is valid for the given account type.
    /// </summary>
    public static bool IsValidCategory(AccountType accountType, string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return false;

        return accountType switch
        {
            AccountType.Financial  => FinancialCategories.Contains(category),
            AccountType.Statistical => StatisticalCategories.Contains(category),
            _ => false
        };
    }

    /// <summary>Returns the allowed categories for an account type.</summary>
    public static IReadOnlySet<string> GetAllowedCategories(AccountType accountType) =>
        accountType switch
        {
            AccountType.Financial   => FinancialCategories,
            AccountType.Statistical => StatisticalCategories,
            _ => new HashSet<string>()
        };
}
