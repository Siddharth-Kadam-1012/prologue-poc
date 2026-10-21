using GlAccounts.Domain.Enums;

namespace GlAccounts.Domain.Entities;

/// <summary>
/// Core General Ledger account entity.
/// AccountId and AccountType are permanent after creation (BR-GLA-001, BR-GLA-003).
/// </summary>
public sealed class GlAccount
{
    public required string AccountId { get; init; }
    public required string Description { get; set; }
    public required AccountType AccountType { get; init; }
    public required string Category { get; set; }
    public required AccountStatus Status { get; set; }
    public required BalanceType BalanceType { get; set; }
    public required NormalBalance NormalBalance { get; set; }
    public string? CurrencyId { get; set; }
    public required DateOnly CreatedDate { get; init; }
    public required string CreatedBy { get; init; }
}
