using GlAccounts.Application.Ports;

namespace GlAccounts.Application.Features.GlAccounts.Transactions;

public sealed class TransactionsHandler
{
    private readonly IAccountStore _accounts;
    private readonly ITransactionStore _transactions;

    public TransactionsHandler(IAccountStore accounts, ITransactionStore transactions)
    {
        _accounts     = accounts;
        _transactions = transactions;
    }

    /// <summary>Returns null if the account does not exist.</summary>
    public async Task<TransactionsResult?> HandleAsync(TransactionsQuery query, CancellationToken ct = default)
    {
        if (!await _accounts.ExistsAsync(query.AccountId, ct)) return null;

        var all = await _transactions.GetByAccountIdAsync(query.AccountId, ct);

        var filtered = all.AsEnumerable();
        if (query.FromDate.HasValue)
            filtered = filtered.Where(t => t.PostingDate >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            filtered = filtered.Where(t => t.PostingDate <= query.ToDate.Value);
        if (query.PostingStatus.HasValue)
            filtered = filtered.Where(t => t.PostingStatus == query.PostingStatus.Value);

        var items = filtered.Select(t => new TransactionItem(
            t.TransactionId, t.AccountId, t.BatchId, t.Description,
            t.Amount, t.PostingDate, t.PostingStatus)).ToList();

        return new TransactionsResult(query.AccountId, items, items.Count);
    }
}
