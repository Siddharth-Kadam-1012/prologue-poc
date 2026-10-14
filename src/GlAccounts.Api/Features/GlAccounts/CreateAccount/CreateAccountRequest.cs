using System.Text.Json.Serialization;
using GlAccounts.Api.Domain;

namespace GlAccounts.Api.Features.GlAccounts.CreateAccount;

public record AttributeInput(
    [property: JsonPropertyName("attributeTypeId")] string AttributeTypeId,
    [property: JsonPropertyName("attributeValueId")] string AttributeValueId);

public class CreateAccountRequest
{
    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    [JsonPropertyName("templateAccountId")]
    public string? TemplateAccountId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("accountType")]
    public AccountType? AccountType { get; set; }

    [JsonPropertyName("category")]
    public Category? Category { get; set; }

    [JsonPropertyName("currencyId")]
    public string? CurrencyId { get; set; }

    [JsonPropertyName("status")]
    public AccountStatus? Status { get; set; }

    [JsonPropertyName("balanceType")]
    public BalanceType? BalanceType { get; set; }

    [JsonPropertyName("normalBalance")]
    public NormalBalance? NormalBalance { get; set; }

    [JsonPropertyName("attributes")]
    public List<AttributeInput>? Attributes { get; set; }
}
