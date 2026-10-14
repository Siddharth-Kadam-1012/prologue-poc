using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.CreateAccount;

public record ValidationOutcome(
    bool IsValid,
    IReadOnlyList<Segment>? Segments,
    Dictionary<string, string[]> Errors);

public static class CreateAccountValidator
{
    private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "JPY"
    };

    public static ValidationOutcome Validate(CreateAccountRequest request)
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

        // Account ID
        IReadOnlyList<Segment>? segments = null;
        if (string.IsNullOrWhiteSpace(request.AccountId))
        {
            AddError("accountId", "Account ID is required.");
        }
        else
        {
            var segmentResult = AccountIdParser.Validate(request.AccountId);
            if (!segmentResult.IsValid)
            {
                foreach (var err in segmentResult.Errors)
                {
                    AddError("accountId", err);
                }
            }
            else
            {
                segments = segmentResult.Segments;
            }
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

        // Account Type
        if (!request.AccountType.HasValue)
        {
            AddError("accountType", "Account type is required.");
        }

        // Category
        if (!request.Category.HasValue)
        {
            AddError("category", "Category is required.");
        }

        // Currency ID
        if (string.IsNullOrWhiteSpace(request.CurrencyId))
        {
            AddError("currencyId", "Currency ID is required.");
        }
        else if (!AllowedCurrencies.Contains(request.CurrencyId))
        {
            AddError("currencyId", $"Currency ID '{request.CurrencyId}' is invalid. Allowed currencies: USD, EUR, GBP, JPY.");
        }

        // Status
        if (!request.Status.HasValue)
        {
            AddError("status", "Status is required.");
        }

        // Classification & Balances rules
        if (request.AccountType.HasValue && request.Category.HasValue)
        {
            var (isClassificationValid, classErrors) = ClassificationRules.ValidateClassification(
                request.AccountType.Value,
                request.Category.Value,
                request.BalanceType,
                request.NormalBalance);

            if (!isClassificationValid)
            {
                foreach (var err in classErrors)
                {
                    if (err.Contains("Category", StringComparison.OrdinalIgnoreCase))
                    {
                        AddError("category", err);
                    }
                    else if (err.Contains("Balance type", StringComparison.OrdinalIgnoreCase))
                    {
                        AddError("balanceType", err);
                    }
                    else if (err.Contains("Normal balance", StringComparison.OrdinalIgnoreCase))
                    {
                        AddError("normalBalance", err);
                    }
                    else
                    {
                        AddError("classification", err);
                    }
                }
            }
        }
        else if (request.AccountType.HasValue)
        {
            if (request.AccountType.Value == AccountType.Financial)
            {
                if (!request.BalanceType.HasValue) AddError("balanceType", "Balance type is required for Financial accounts.");
                if (!request.NormalBalance.HasValue) AddError("normalBalance", "Normal balance is required for Financial accounts.");
            }
            else if (request.AccountType.Value == AccountType.Statistical)
            {
                if (request.BalanceType.HasValue) AddError("balanceType", "Balance type must not be specified for Statistical accounts.");
                if (request.NormalBalance.HasValue) AddError("normalBalance", "Normal balance must not be specified for Statistical accounts.");
            }
        }

        // Attributes validation
        if (request.Attributes != null)
        {
            for (int i = 0; i < request.Attributes.Count; i++)
            {
                var attr = request.Attributes[i];
                if (string.IsNullOrWhiteSpace(attr.AttributeTypeId))
                {
                    AddError($"attributes[{i}].attributeTypeId", "Attribute type ID is required.");
                }
                if (string.IsNullOrWhiteSpace(attr.AttributeValueId))
                {
                    AddError($"attributes[{i}].attributeValueId", "Attribute value ID is required.");
                }
            }
        }

        var dict = errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        return new ValidationOutcome(dict.Count == 0, segments, dict);
    }
}
