using System.Text.Json.Serialization;

namespace GlAccounts.Api.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountType
{
    Financial,
    Statistical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Category
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
    Statistical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CurrencyId
{
    USD,
    EUR,
    GBP,
    JPY
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountStatus
{
    Active,
    Hold,
    Inactive,
    Template
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BalanceType
{
    BalanceForward,
    Periodic
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NormalBalance
{
    Debit,
    Credit
}

public record Segment(string Name, string Value);

public record AccountAttribute(string AttributeId, string AttributeTypeId, string AttributeValueId);

public class Account
{
    public required string AccountId { get; init; }
    public required IReadOnlyList<Segment> Segments { get; init; }
    public required string Description { get; set; }
    public required AccountType AccountType { get; init; }
    public required Category Category { get; set; }
    public required string CurrencyId { get; set; }
    public required AccountStatus Status { get; set; }
    public BalanceType? BalanceType { get; init; }
    public NormalBalance? NormalBalance { get; init; }
    public required DateOnly CreatedDate { get; init; }
    public required string Version { get; set; }
    public bool EligibleForPosting => Status == AccountStatus.Active;
    public List<AccountAttribute> Attributes { get; set; } = new();

    public Account Clone()
    {
        return new Account
        {
            AccountId = AccountId,
            Segments = Segments.Select(s => new Segment(s.Name, s.Value)).ToList(),
            Description = Description,
            AccountType = AccountType,
            Category = Category,
            CurrencyId = CurrencyId,
            Status = Status,
            BalanceType = BalanceType,
            NormalBalance = NormalBalance,
            CreatedDate = CreatedDate,
            Version = Version,
            Attributes = Attributes.Select(a => new AccountAttribute(a.AttributeId, a.AttributeTypeId, a.AttributeValueId)).ToList()
        };
    }
}
