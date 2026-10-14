using System.Text.Json.Serialization;
using GlAccounts.Api.Domain;

namespace GlAccounts.Api.Features.GlAccounts.UpdateAccount;

public record AttributeChangeInput(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("attributeId")] string? AttributeId = null,
    [property: JsonPropertyName("attributeTypeId")] string? AttributeTypeId = null,
    [property: JsonPropertyName("attributeValueId")] string? AttributeValueId = null);

public class UpdateAccountRequest
{
    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    [JsonPropertyName("accountType")]
    public AccountType? AccountType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public Category? Category { get; set; }

    [JsonPropertyName("currencyId")]
    public string? CurrencyId { get; set; }

    [JsonPropertyName("status")]
    public AccountStatus? Status { get; set; }

    [JsonPropertyName("statusJustification")]
    public string? StatusJustification { get; set; }

    [JsonPropertyName("attributeChanges")]
    public List<AttributeChangeInput>? AttributeChanges { get; set; }
}
