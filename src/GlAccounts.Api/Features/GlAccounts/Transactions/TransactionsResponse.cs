using System.Text.Json.Serialization;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.Transactions;

public class JournalEntryPage
{
    [JsonPropertyName("items")]
    public required IReadOnlyList<JournalEntrySummary> Items { get; init; }

    [JsonPropertyName("total")]
    public required int Total { get; init; }

    [JsonPropertyName("page")]
    public required int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public required int PageSize { get; init; }
}
