using ClosedXML.Excel;
using GlAccounts.Api.Features.GlAccounts.PeriodSummary;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.UnitTests;

public class PeriodSummaryTests
{
    private readonly InMemoryAccountStore _accountStore;
    private readonly InMemoryPeriodBalanceStore _periodBalanceStore;
    private readonly PeriodSummaryHandler _handler;

    public PeriodSummaryTests()
    {
        _accountStore = new InMemoryAccountStore();
        _periodBalanceStore = new InMemoryPeriodBalanceStore();
        _handler = new PeriodSummaryHandler(_accountStore, _periodBalanceStore);
    }

    [Fact]
    public async Task AC_GLA_028_AC_GLA_029_GetPeriodSummary_ExistingAccount_ReturnsBalancesAndAnnualTotals()
    {
        // REQ-GLA-017, AC-GLA-028, AC-GLA-029
        var result = await _handler.GetSummaryAsync("1000-100-01", null, null, null);

        var success = Assert.IsType<PeriodSummaryResult.Success>(result);
        var summary = success.Summary;

        Assert.Equal("1000-100-01", summary.AccountId);
        Assert.NotEmpty(summary.Periods);
        Assert.NotEmpty(summary.Years);
        Assert.NotEmpty(summary.BudgetVersions);

        Assert.Contains(summary.Years, y => y.FiscalYear == 2025 && y.Balance == 159000m);
        Assert.Contains(summary.BudgetVersions, b => b.BudgetVersion == "Original" && b.FiscalYear == 2025);
    }

    [Fact]
    public async Task AC_GLA_030_GetPeriodSummary_FilterByFiscalYear_ReturnsOnlyMatchingYear()
    {
        // REQ-GLA-018, AC-GLA-030
        var result = await _handler.GetSummaryAsync("1000-100-01", 2025, null, null);

        var success = Assert.IsType<PeriodSummaryResult.Success>(result);
        var summary = success.Summary;

        Assert.All(summary.Periods, p => Assert.Equal(2025, p.FiscalYear));
        Assert.All(summary.Years, y => Assert.Equal(2025, y.FiscalYear));
        Assert.All(summary.BudgetVersions, b => Assert.Equal(2025, b.FiscalYear));
    }

    [Fact]
    public async Task AC_GLA_031_GetPeriodSummary_SortByBalanceDesc_ReturnsSorted()
    {
        // REQ-GLA-018, AC-GLA-031
        var result = await _handler.GetSummaryAsync("1000-100-01", null, "balance", "desc");

        var success = Assert.IsType<PeriodSummaryResult.Success>(result);
        var periods = success.Summary.Periods;

        for (int i = 0; i < periods.Count - 1; i++)
        {
            Assert.True(periods[i].Balance >= periods[i + 1].Balance);
        }
    }

    [Fact]
    public async Task AC_GLA_032_ExportPeriodSummary_GeneratesValidExcelWorkbook()
    {
        // REQ-GLA-019, AC-GLA-032
        var result = await _handler.ExportSummaryAsync("1000-100-01", 2025, null, null);

        var export = Assert.IsType<PeriodSummaryResult.ExportSuccess>(result);
        Assert.Equal("period-summary-1000-100-01.xlsx", export.FileName);
        Assert.NotEmpty(export.Content);

        // Verify valid excel content using ClosedXML
        using var ms = new MemoryStream(export.Content);
        using var workbook = new XLWorkbook(ms);
        var ws = workbook.Worksheet("Period Summary");
        Assert.NotNull(ws);
        Assert.Equal("1000-100-01", ws.Cell(1, 2).GetString());
    }

    [Fact]
    public async Task GetPeriodSummary_NonExistentAccount_ReturnsNotFound()
    {
        var result = await _handler.GetSummaryAsync("9999-999-99", null, null, null);

        var notFound = Assert.IsType<PeriodSummaryResult.NotFound>(result);
        Assert.Equal("9999-999-99", notFound.AccountId);
    }
}
