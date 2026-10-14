using ClosedXML.Excel;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.Features.GlAccounts.PeriodSummary;

public abstract record PeriodSummaryResult
{
    public sealed record Success(StubData.PeriodSummary Summary) : PeriodSummaryResult;
    public sealed record ExportSuccess(byte[] Content, string FileName) : PeriodSummaryResult;
    public sealed record NotFound(string AccountId) : PeriodSummaryResult;
    public sealed record InvalidRequest(string Field, string Message) : PeriodSummaryResult;
}

public class PeriodSummaryHandler
{
    private readonly IAccountStore _accountStore;
    private readonly IPeriodBalanceStore _periodBalanceStore;

    private static readonly HashSet<string> ValidSortBy = new(StringComparer.OrdinalIgnoreCase)
    {
        "fiscalYear", "fiscalPeriod", "balance"
    };

    private static readonly HashSet<string> ValidSortDirection = new(StringComparer.OrdinalIgnoreCase)
    {
        "asc", "desc"
    };

    public PeriodSummaryHandler(IAccountStore accountStore, IPeriodBalanceStore periodBalanceStore)
    {
        _accountStore = accountStore;
        _periodBalanceStore = periodBalanceStore;
    }

    public async Task<PeriodSummaryResult> GetSummaryAsync(
        string? id,
        int? fiscalYear,
        string? sortBy,
        string? sortDirection,
        CancellationToken ct = default)
    {
        var validation = ValidateParameters(id, fiscalYear, sortBy, sortDirection);
        if (validation != null)
        {
            return validation;
        }

        var account = await _accountStore.GetByIdAsync(id!, ct);
        if (account is null)
        {
            return new PeriodSummaryResult.NotFound(id!);
        }

        var fullSummary = await _periodBalanceStore.GetPeriodSummaryAsync(id!, ct);
        if (fullSummary is null)
        {
            // Empty summary if no balances exist for this account
            return new PeriodSummaryResult.Success(new StubData.PeriodSummary(
                id!,
                Array.Empty<StubData.PeriodBalance>(),
                Array.Empty<StubData.YearBalance>(),
                Array.Empty<StubData.BudgetVersionAmount>()));
        }

        var filteredPeriods = await _periodBalanceStore.GetFilteredBalancesAsync(
            id!,
            fiscalYear,
            sortBy ?? "fiscalPeriod",
            sortDirection ?? "asc",
            ct);

        var years = fiscalYear.HasValue
            ? fullSummary.Years.Where(y => y.FiscalYear == fiscalYear.Value).ToList()
            : fullSummary.Years;

        var budgetVersions = fiscalYear.HasValue
            ? fullSummary.BudgetVersions.Where(b => b.FiscalYear == fiscalYear.Value).ToList()
            : fullSummary.BudgetVersions;

        var resultSummary = new StubData.PeriodSummary(
            id!,
            filteredPeriods,
            years,
            budgetVersions);

        return new PeriodSummaryResult.Success(resultSummary);
    }

    public async Task<PeriodSummaryResult> ExportSummaryAsync(
        string? id,
        int? fiscalYear,
        string? sortBy,
        string? sortDirection,
        CancellationToken ct = default)
    {
        var summaryResult = await GetSummaryAsync(id, fiscalYear, sortBy, sortDirection, ct);
        if (summaryResult is not PeriodSummaryResult.Success success)
        {
            return summaryResult;
        }

        var summary = success.Summary;

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Period Summary");

        // Header
        ws.Cell(1, 1).Value = "Account ID";
        ws.Cell(1, 2).Value = summary.AccountId;

        ws.Cell(3, 1).Value = "Fiscal Year";
        ws.Cell(3, 2).Value = "Fiscal Period";
        ws.Cell(3, 3).Value = "Balance";

        var row = 4;
        foreach (var period in summary.Periods)
        {
            ws.Cell(row, 1).Value = period.FiscalYear;
            ws.Cell(row, 2).Value = period.FiscalPeriod;
            ws.Cell(row, 3).Value = period.Balance;
            row++;
        }

        row += 2;
        ws.Cell(row, 1).Value = "Annual Balances";
        row++;
        ws.Cell(row, 1).Value = "Fiscal Year";
        ws.Cell(row, 2).Value = "Total Balance";
        row++;
        foreach (var year in summary.Years)
        {
            ws.Cell(row, 1).Value = year.FiscalYear;
            ws.Cell(row, 2).Value = year.Balance;
            row++;
        }

        row += 2;
        ws.Cell(row, 1).Value = "Budget Versions";
        row++;
        ws.Cell(row, 1).Value = "Budget Version";
        ws.Cell(row, 2).Value = "Fiscal Year";
        ws.Cell(row, 3).Value = "Amount";
        row++;
        foreach (var bv in summary.BudgetVersions)
        {
            ws.Cell(row, 1).Value = bv.BudgetVersion;
            ws.Cell(row, 2).Value = bv.FiscalYear;
            ws.Cell(row, 3).Value = bv.Amount;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var fileName = $"period-summary-{id}.xlsx";
        return new PeriodSummaryResult.ExportSuccess(bytes, fileName);
    }

    private static PeriodSummaryResult? ValidateParameters(
        string? id,
        int? fiscalYear,
        string? sortBy,
        string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new PeriodSummaryResult.InvalidRequest("id", "Account ID is required.");
        }

        if (id.Length > AccountIdParser.MaxLength)
        {
            return new PeriodSummaryResult.InvalidRequest("id", $"Account ID must not exceed {AccountIdParser.MaxLength} characters.");
        }

        if (fiscalYear.HasValue && (fiscalYear.Value < 1900 || fiscalYear.Value > 2999))
        {
            return new PeriodSummaryResult.InvalidRequest("fiscalYear", "Fiscal year must be between 1900 and 2999.");
        }

        if (!string.IsNullOrEmpty(sortBy) && !ValidSortBy.Contains(sortBy))
        {
            return new PeriodSummaryResult.InvalidRequest("sortBy", $"Sort column '{sortBy}' is invalid. Allowed: fiscalYear, fiscalPeriod, balance.");
        }

        if (!string.IsNullOrEmpty(sortDirection) && !ValidSortDirection.Contains(sortDirection))
        {
            return new PeriodSummaryResult.InvalidRequest("sortDirection", $"Sort direction '{sortDirection}' is invalid. Allowed: asc, desc.");
        }

        return null;
    }
}
