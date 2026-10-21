using GlAccounts.Domain.Enums;

namespace GlAccounts.Domain.Entities;

/// <summary>
/// General Ledger journal transaction — read model managed by the journal module.
/// This feature only reads transaction data.
/// </summary>
public sealed class GlTransaction
{
    public required string TransactionId { get; init; }
    public required string AccountId { get; init; }
    public required string BatchId { get; init; }
    public required string Description { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly PostingDate { get; init; }
    public required PostingStatus PostingStatus { get; init; }
}
