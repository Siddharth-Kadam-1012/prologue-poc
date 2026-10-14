using System.Text.Json.Serialization;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.ListAccountGroups;

public class AccountGroupListResponse
{
    [JsonPropertyName("items")]
    public required IReadOnlyList<AccountGroup> Items { get; init; }
}
