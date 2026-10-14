using GlAccounts.Api.Domain;

namespace GlAccounts.Api.Features.GlAccounts.UpdateAccount;

public record UpdateValidationOutcome(
    bool IsValid,
    Dictionary<string, string[]> Errors);

public static class UpdateAccountValidator
{
    private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "JPY"
    };

    public static UpdateValidationOutcome Validate(
        string pathId,
        string? ifMatch,
        UpdateAccountRequest request,
        Account existingAccount)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string field, string message)
        {
            if (!errors.TryGetValue(field, out var list))
            {
                list = new List<string>();
                errors[field] = list;
            }
            list.Add(message);
        }

        // If-Match
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            AddError("If-Match", "If-Match header is required.");
        }

        // Immutability checks (REQ-GLA-004, AC-GLA-008, AC-GLA-045)
        if (!string.IsNullOrWhiteSpace(request.AccountId) &&
            !string.Equals(request.AccountId, existingAccount.AccountId, StringComparison.OrdinalIgnoreCase))
        {
            AddError("accountId", "Account ID is permanent and cannot be modified.");
        }

        if (request.AccountType.HasValue && request.AccountType.Value != existingAccount.AccountType)
        {
            AddError("accountType", "Account Type is permanent and cannot be modified.");
        }

        // Description
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            AddError("description", "Description is required.");
        }
        else if (request.Description.Length > 200)
        {
            AddError("description", "Description must be between 1 and 200 characters.");
        }

        // Category
        if (!request.Category.HasValue)
        {
            AddError("category", "Category is required.");
        }
        else if (!ClassificationRules.IsCategoryValidForAccountType(existingAccount.AccountType, request.Category.Value))
        {
            if (existingAccount.AccountType == AccountType.Financial)
            {
                AddError("category", $"Category '{request.Category.Value}' is invalid for Financial accounts. Allowed categories: {string.Join(", ", ClassificationRules.FinancialCategories)}.");
            }
            else
            {
                AddError("category", $"Category '{request.Category.Value}' is invalid for Statistical accounts. Allowed category: Statistical.");
            }
        }

        // Currency ID (REQ-GLA-007, Q-011)
        if (string.IsNullOrWhiteSpace(request.CurrencyId))
        {
            AddError("currencyId", "Currency ID is required.");
        }
        else if (!AllowedCurrencies.Contains(request.CurrencyId))
        {
            AddError("currencyId", $"Currency ID '{request.CurrencyId}' is invalid. Allowed currencies: USD, EUR, GBP, JPY.");
        }

        // Status & Transition rules (REQ-GLA-014, AC-GLA-022, AC-GLA-023)
        if (!request.Status.HasValue)
        {
            AddError("status", "Status is required.");
        }
        else if (request.Status.Value != existingAccount.Status)
        {
            var (isValid, error) = StatusRules.ValidateStatusTransition(
                existingAccount.Status,
                request.Status.Value,
                request.StatusJustification);

            if (!isValid && error != null)
            {
                if (error.Contains("justification", StringComparison.OrdinalIgnoreCase))
                {
                    AddError("statusJustification", error);
                }
                else
                {
                    AddError("status", error);
                }
            }
        }

        var dict = errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        return new UpdateValidationOutcome(dict.Count == 0, dict);
    }
}
