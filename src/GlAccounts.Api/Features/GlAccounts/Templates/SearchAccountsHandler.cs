using System.Text.Json.Serialization;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.Templates;

public record AccountSummary(
    [property: JsonPropertyName("accountId")] string AccountId,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("accountType")] AccountType AccountType,
    [property: JsonPropertyName("category")] Category Category,
    [property: JsonPropertyName("currencyId")] string CurrencyId,
    [property: JsonPropertyName("status")] AccountStatus Status);

public record AccountSummaryPage(
    [property: JsonPropertyName("items")] IReadOnlyList<AccountSummary> Items,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize);

public abstract record SearchAccountsResult
{
    public sealed record Success(AccountSummaryPage Page) : SearchAccountsResult;
    public sealed record InvalidRequest(string Field, string Message) : SearchAccountsResult;
}

public class SearchAccountsHandler
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;
    private const int MaxQueryLength = 200;

    private readonly IAccountStore _accountStore;

    public SearchAccountsHandler(IAccountStore accountStore)
    {
        _accountStore = accountStore;
    }

    public async Task<SearchAccountsResult> HandleAsync(
        string? q,
        string? status,
        string? accountType,
        string? category,
        int? page,
        int? pageSize,
        CancellationToken ct = default)
    {
        if (q is not null && q.Length > MaxQueryLength)
        {
            return new SearchAccountsResult.InvalidRequest("q", $"Search text must not exceed {MaxQueryLength} characters.");
        }

        AccountStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<AccountStatus>(status, ignoreCase: false, out var parsed) || !Enum.IsDefined(parsed))
            {
                return new SearchAccountsResult.InvalidRequest("status", $"Status '{status}' is invalid. Allowed values: {string.Join(", ", Enum.GetNames<AccountStatus>())}.");
            }
            statusFilter = parsed;
        }

        AccountType? typeFilter = null;
        if (!string.IsNullOrWhiteSpace(accountType))
        {
            if (!Enum.TryParse<AccountType>(accountType, ignoreCase: false, out var parsed) || !Enum.IsDefined(parsed))
            {
                return new SearchAccountsResult.InvalidRequest("accountType", $"Account type '{accountType}' is invalid. Allowed values: {string.Join(", ", Enum.GetNames<AccountType>())}.");
            }
            typeFilter = parsed;
        }

        Category? categoryFilter = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<Category>(category, ignoreCase: false, out var parsed) || !Enum.IsDefined(parsed))
            {
                return new SearchAccountsResult.InvalidRequest("category", $"Category '{category}' is invalid. Allowed values: {string.Join(", ", Enum.GetNames<Category>())}.");
            }
            categoryFilter = parsed;
        }

        var effectivePage = page ?? 1;
        var effectivePageSize = pageSize ?? DefaultPageSize;

        if (effectivePage < 1)
        {
            return new SearchAccountsResult.InvalidRequest("page", "Page number must be at least 1.");
        }

        if (effectivePageSize < 1 || effectivePageSize > MaxPageSize)
        {
            return new SearchAccountsResult.InvalidRequest("pageSize", $"Page size must be between 1 and {MaxPageSize}.");
        }

        var matches = await _accountStore.SearchAsync(q, statusFilter, typeFilter, categoryFilter, ct);

        var items = matches
            .Skip((effectivePage - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .Select(a => new AccountSummary(a.AccountId, a.Description, a.AccountType, a.Category, a.CurrencyId, a.Status))
            .ToList();

        return new SearchAccountsResult.Success(
            new AccountSummaryPage(items, matches.Count, effectivePage, effectivePageSize));
    }
}
