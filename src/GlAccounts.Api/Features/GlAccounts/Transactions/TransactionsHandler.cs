using System.Text;
using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.Transactions;

public abstract record TransactionsResult
{
    public sealed record PageSuccess(JournalEntryPage Page) : TransactionsResult;
    public sealed record ExportSuccess(byte[] Content, string FileName) : TransactionsResult;
    public sealed record DetailSuccess(JournalEntryDetail Detail) : TransactionsResult;
    public sealed record NotFound(string Id) : TransactionsResult;
    public sealed record InvalidRequest(string Field, string Message) : TransactionsResult;
}

public class TransactionsHandler
{
    private readonly IAccountStore _accountStore;
    private readonly ITransactionStore _transactionStore;

    public TransactionsHandler(IAccountStore accountStore, ITransactionStore transactionStore)
    {
        _accountStore = accountStore;
        _transactionStore = transactionStore;
    }

    public async Task<TransactionsResult> ListTransactionsAsync(
        string? id,
        DateOnly? fromDate,
        DateOnly? toDate,
        int? fiscalYear,
        int? fiscalPeriod,
        int? page,
        int? pageSize,
        CancellationToken ct = default)
    {
        var validation = ValidateParameters(id, fromDate, toDate, fiscalYear, fiscalPeriod, page, pageSize, out var effectivePage, out var effectivePageSize);
        if (validation != null)
        {
            return validation;
        }

        var account = await _accountStore.GetByIdAsync(id!, ct);
        if (account is null)
        {
            return new TransactionsResult.NotFound(id!);
        }

        var (items, total) = await _transactionStore.GetTransactionsAsync(
            id!,
            fromDate,
            toDate,
            fiscalYear,
            fiscalPeriod,
            effectivePage,
            effectivePageSize,
            ct);

        var pageResult = new JournalEntryPage
        {
            Items = items,
            Total = total,
            Page = effectivePage,
            PageSize = effectivePageSize
        };

        return new TransactionsResult.PageSuccess(pageResult);
    }

    public async Task<TransactionsResult> ExportTransactionsAsync(
        string? id,
        DateOnly? fromDate,
        DateOnly? toDate,
        int? fiscalYear,
        int? fiscalPeriod,
        CancellationToken ct = default)
    {
        var validation = ValidateParameters(id, fromDate, toDate, fiscalYear, fiscalPeriod, 1, 50, out _, out _);
        if (validation != null)
        {
            return validation;
        }

        var account = await _accountStore.GetByIdAsync(id!, ct);
        if (account is null)
        {
            return new TransactionsResult.NotFound(id!);
        }

        var items = await _transactionStore.GetTransactionsForExportAsync(
            id!,
            fromDate,
            toDate,
            fiscalYear,
            fiscalPeriod,
            ct);

        var sb = new StringBuilder();
        sb.AppendLine("entryId,batchId,description,amount,postingStatus,postingDate");

        foreach (var item in items)
        {
            var escapedDesc = EscapeCsv(item.Description);
            sb.AppendLine($"{item.EntryId},{item.BatchId},{escapedDesc},{item.Amount:F2},{item.PostingStatus},{item.PostingDate:yyyy-MM-dd}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"transactions-{id}.csv";

        return new TransactionsResult.ExportSuccess(bytes, fileName);
    }

    public async Task<TransactionsResult> GetDetailAsync(
        string? id,
        string? entryId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > AccountIdParser.MaxLength)
        {
            return new TransactionsResult.InvalidRequest("id", "Invalid account ID.");
        }

        if (string.IsNullOrWhiteSpace(entryId) || entryId.Length > 64)
        {
            return new TransactionsResult.InvalidRequest("entryId", "Invalid entry ID.");
        }

        var account = await _accountStore.GetByIdAsync(id, ct);
        if (account is null)
        {
            return new TransactionsResult.NotFound(id);
        }

        var detail = await _transactionStore.GetTransactionDetailAsync(id, entryId, ct);
        if (detail is null)
        {
            return new TransactionsResult.NotFound(entryId);
        }

        return new TransactionsResult.DetailSuccess(detail);
    }

    private static TransactionsResult? ValidateParameters(
        string? id,
        DateOnly? fromDate,
        DateOnly? toDate,
        int? fiscalYear,
        int? fiscalPeriod,
        int? page,
        int? pageSize,
        out int effectivePage,
        out int effectivePageSize)
    {
        effectivePage = page ?? 1;
        effectivePageSize = pageSize ?? 50;

        if (string.IsNullOrWhiteSpace(id))
        {
            return new TransactionsResult.InvalidRequest("id", "Account ID is required.");
        }

        if (id.Length > AccountIdParser.MaxLength)
        {
            return new TransactionsResult.InvalidRequest("id", $"Account ID must not exceed {AccountIdParser.MaxLength} characters.");
        }

        if (effectivePage < 1)
        {
            return new TransactionsResult.InvalidRequest("page", "Page number must be at least 1.");
        }

        if (effectivePageSize < 1 || effectivePageSize > 200)
        {
            return new TransactionsResult.InvalidRequest("pageSize", "Page size must be between 1 and 200.");
        }

        var hasDateFilter = fromDate.HasValue || toDate.HasValue;
        var hasFiscalFilter = fiscalYear.HasValue || fiscalPeriod.HasValue;

        // Mutual exclusivity (AC-GLA-035)
        if (hasDateFilter && hasFiscalFilter)
        {
            return new TransactionsResult.InvalidRequest("filter", "Filter by either date range or fiscal period, not both.");
        }

        if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
        {
            return new TransactionsResult.InvalidRequest("toDate", "toDate cannot be earlier than fromDate.");
        }

        if (fiscalPeriod.HasValue && !fiscalYear.HasValue)
        {
            return new TransactionsResult.InvalidRequest("fiscalYear", "fiscalYear is required when fiscalPeriod is specified.");
        }

        if (fiscalYear.HasValue && (fiscalYear.Value < 1900 || fiscalYear.Value > 2999))
        {
            return new TransactionsResult.InvalidRequest("fiscalYear", "Fiscal year must be between 1900 and 2999.");
        }

        if (fiscalPeriod.HasValue && (fiscalPeriod.Value < 1 || fiscalPeriod.Value > 13))
        {
            return new TransactionsResult.InvalidRequest("fiscalPeriod", "Fiscal period must be between 1 and 13.");
        }

        return null;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
