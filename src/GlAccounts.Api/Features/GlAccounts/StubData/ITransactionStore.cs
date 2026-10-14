namespace GlAccounts.Api.Features.GlAccounts.StubData;

public record JournalLine(
    int LineNumber,
    string AccountId,
    string Description,
    decimal Debit,
    decimal Credit);

public record JournalEntrySummary(
    string EntryId,
    string BatchId,
    string Description,
    decimal Amount,
    string PostingStatus,
    DateOnly PostingDate);

public record JournalEntryDetail(
    string EntryId,
    string BatchId,
    string Description,
    decimal Amount,
    string PostingStatus,
    DateOnly PostingDate,
    int FiscalYear,
    int FiscalPeriod,
    IReadOnlyList<JournalLine> Lines);

public interface ITransactionStore
{
    Task<(IReadOnlyList<JournalEntrySummary> Items, int Total)> GetTransactionsAsync(
        string accountId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int? fiscalYear,
        int? fiscalPeriod,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<JournalEntryDetail?> GetTransactionDetailAsync(
        string accountId,
        string entryId,
        CancellationToken ct = default);

    Task<IReadOnlyList<JournalEntrySummary>> GetTransactionsForExportAsync(
        string accountId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int? fiscalYear,
        int? fiscalPeriod,
        CancellationToken ct = default);
}
