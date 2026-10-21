using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.Transactions;

public sealed record TransactionsQuery(
    string AccountId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    PostingStatus? PostingStatus);

public sealed record TransactionItem(
    string TransactionId,
    string AccountId,
    string BatchId,
    string Description,
    decimal Amount,
    DateOnly PostingDate,
    PostingStatus PostingStatus);

public sealed record TransactionsResult(
    string AccountId,
    IReadOnlyList<TransactionItem> Items,
    int Total);
