namespace GlAccounts.Api.Features.GlAccounts.StubData;

public class InMemoryTransactionStore : ITransactionStore
{
    private static readonly Dictionary<string, List<JournalEntryDetail>> AccountTransactions = InitializeTransactions();

    public Task<(IReadOnlyList<JournalEntrySummary> Items, int Total)> GetTransactionsAsync(
        string accountId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int? fiscalYear,
        int? fiscalPeriod,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var filtered = FilterTransactions(accountId, fromDate, toDate, fiscalYear, fiscalPeriod);
        var total = filtered.Count;
        var paged = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new JournalEntrySummary(
                t.EntryId,
                t.BatchId,
                t.Description,
                t.Amount,
                t.PostingStatus,
                t.PostingDate))
            .ToList();

        return Task.FromResult<(IReadOnlyList<JournalEntrySummary>, int)>((paged, total));
    }

    public Task<JournalEntryDetail?> GetTransactionDetailAsync(
        string accountId,
        string entryId,
        CancellationToken ct = default)
    {
        if (AccountTransactions.TryGetValue(accountId, out var list))
        {
            var entry = list.FirstOrDefault(e => string.Equals(e.EntryId, entryId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(entry);
        }

        return Task.FromResult<JournalEntryDetail?>(null);
    }

    public Task<IReadOnlyList<JournalEntrySummary>> GetTransactionsForExportAsync(
        string accountId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int? fiscalYear,
        int? fiscalPeriod,
        CancellationToken ct = default)
    {
        var filtered = FilterTransactions(accountId, fromDate, toDate, fiscalYear, fiscalPeriod);
        var summaries = filtered.Select(t => new JournalEntrySummary(
            t.EntryId,
            t.BatchId,
            t.Description,
            t.Amount,
            t.PostingStatus,
            t.PostingDate)).ToList();

        return Task.FromResult<IReadOnlyList<JournalEntrySummary>>(summaries);
    }

    private static List<JournalEntryDetail> FilterTransactions(
        string accountId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int? fiscalYear,
        int? fiscalPeriod)
    {
        if (!AccountTransactions.TryGetValue(accountId, out var list))
        {
            return new List<JournalEntryDetail>();
        }

        var query = list.AsEnumerable();

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.PostingDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.PostingDate <= toDate.Value);
        }

        if (fiscalYear.HasValue)
        {
            query = query.Where(t => t.FiscalYear == fiscalYear.Value);
        }

        if (fiscalPeriod.HasValue)
        {
            query = query.Where(t => t.FiscalPeriod == fiscalPeriod.Value);
        }

        return query.OrderByDescending(t => t.PostingDate).ThenByDescending(t => t.EntryId).ToList();
    }

    private static Dictionary<string, List<JournalEntryDetail>> InitializeTransactions()
    {
        var dict = new Dictionary<string, List<JournalEntryDetail>>(StringComparer.OrdinalIgnoreCase);

        // 1000-100-01: 25 transactions
        var list100 = new List<JournalEntryDetail>();
        for (int i = 1; i <= 25; i++)
        {
            var year = 2025 + (i > 20 ? 1 : 0);
            var period = ((i - 1) % 12) + 1;
            var day = Math.Min(i, 28);
            var date = new DateOnly(year, Math.Min(period, 12), day);
            var entryId = $"je-{i:D6}";
            var batchId = $"B-{year}-{i:D4}";

            list100.Add(new JournalEntryDetail(
                EntryId: entryId,
                BatchId: batchId,
                Description: $"Operating transaction {i}",
                Amount: 1000.00m + (i * 10),
                PostingStatus: "Posted",
                PostingDate: date,
                FiscalYear: year,
                FiscalPeriod: period,
                Lines: new List<JournalLine>
                {
                    new(1, "1000-100-01", $"Operating transaction {i}", 1000.00m + (i * 10), 0m),
                    new(2, "3000-100-01", $"Offset {i}", 0m, 1000.00m + (i * 10))
                }));
        }
        dict["1000-100-01"] = list100;

        // 1000-300-01: 5 unposted transactions
        var list300 = new List<JournalEntryDetail>();
        for (int i = 1; i <= 5; i++)
        {
            var entryId = $"unp-{i:D6}";
            var batchId = $"B-2025-U{i:D3}";
            list300.Add(new JournalEntryDetail(
                EntryId: entryId,
                BatchId: batchId,
                Description: $"Unposted payroll entry {i}",
                Amount: 500.00m * i,
                PostingStatus: "Unposted",
                PostingDate: new DateOnly(2025, 1, 10 + i),
                FiscalYear: 2025,
                FiscalPeriod: 1,
                Lines: new List<JournalLine>
                {
                    new(1, "1000-300-01", $"Payroll clearing {i}", 500.00m * i, 0m),
                    new(2, "2000-100-01", $"Payroll liability {i}", 0m, 500.00m * i)
                }));
        }
        dict["1000-300-01"] = list300;

        // 1000-200-01: 10,001 posted transactions
        var list200 = new List<JournalEntryDetail>(10001);
        for (int i = 1; i <= 10001; i++)
        {
            var year = 2025 + ((i - 1) / 5000);
            var period = ((i - 1) % 12) + 1;
            var day = ((i - 1) % 28) + 1;
            var date = new DateOnly(year, period, day);
            var entryId = $"je-{i:D6}";
            var batchId = $"B-{year}-{(i % 1000):D4}";

            list200.Add(new JournalEntryDetail(
                EntryId: entryId,
                BatchId: batchId,
                Description: $"Batch posted transaction {i}",
                Amount: 100.00m + (i % 500),
                PostingStatus: "Posted",
                PostingDate: date,
                FiscalYear: year,
                FiscalPeriod: period,
                Lines: new List<JournalLine>
                {
                    new(1, "1000-200-01", $"Line 1 debit {i}", 100.00m + (i % 500), 0m),
                    new(2, "2000-100-01", $"Line 2 credit {i}", 0m, 100.00m + (i % 500))
                }));
        }
        dict["1000-200-01"] = list200;

        return dict;
    }
}
