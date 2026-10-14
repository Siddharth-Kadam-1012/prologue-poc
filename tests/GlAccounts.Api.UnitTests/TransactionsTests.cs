using System.Text;
using GlAccounts.Api.Features.GlAccounts.StubData;
using GlAccounts.Api.Features.GlAccounts.Transactions;

namespace GlAccounts.Api.UnitTests;

public class TransactionsTests
{
    private readonly InMemoryAccountStore _accountStore;
    private readonly InMemoryTransactionStore _transactionStore;
    private readonly TransactionsHandler _handler;

    public TransactionsTests()
    {
        _accountStore = new InMemoryAccountStore();
        _transactionStore = new InMemoryTransactionStore();
        _handler = new TransactionsHandler(_accountStore, _transactionStore);
    }

    [Fact]
    public async Task AC_GLA_033_ListTransactions_DefaultPaging_ReturnsPageSize50()
    {
        // REQ-GLA-020, AC-GLA-033
        var result = await _handler.ListTransactionsAsync(
            "1000-200-01", null, null, null, null, null, null);

        var success = Assert.IsType<TransactionsResult.PageSuccess>(result);
        Assert.Equal(50, success.Page.PageSize);
        Assert.Equal(1, success.Page.Page);
        Assert.Equal(10001, success.Page.Total);
        Assert.Equal(50, success.Page.Items.Count);
    }

    [Fact]
    public async Task AC_GLA_033_ListTransactions_PageSizeExceeding200_ReturnsInvalidRequest()
    {
        // REQ-GLA-020, AC-GLA-033
        var result = await _handler.ListTransactionsAsync(
            "1000-200-01", null, null, null, null, 1, 250);

        var invalid = Assert.IsType<TransactionsResult.InvalidRequest>(result);
        Assert.Equal("pageSize", invalid.Field);
    }

    [Fact]
    public async Task AC_GLA_034_ListTransactions_DateFilter_ReturnsFilteredAndSorted()
    {
        // REQ-GLA-021, AC-GLA-034
        var fromDate = new DateOnly(2025, 1, 1);
        var toDate = new DateOnly(2025, 1, 15);

        var result = await _handler.ListTransactionsAsync(
            "1000-100-01", fromDate, toDate, null, null, 1, 50);

        var success = Assert.IsType<TransactionsResult.PageSuccess>(result);
        Assert.NotEmpty(success.Page.Items);
        Assert.All(success.Page.Items, i =>
        {
            Assert.True(i.PostingDate >= fromDate);
            Assert.True(i.PostingDate <= toDate);
        });

        // Verify descending sort by posting date
        for (int i = 0; i < success.Page.Items.Count - 1; i++)
        {
            Assert.True(success.Page.Items[i].PostingDate >= success.Page.Items[i + 1].PostingDate);
        }
    }

    [Fact]
    public async Task AC_GLA_035_ListTransactions_BothDateAndFiscalFilter_ReturnsMutualExclusivityError()
    {
        // REQ-GLA-021, AC-GLA-035
        var result = await _handler.ListTransactionsAsync(
            "1000-100-01",
            fromDate: new DateOnly(2025, 1, 1),
            toDate: new DateOnly(2025, 1, 31),
            fiscalYear: 2025,
            fiscalPeriod: 1,
            page: 1,
            pageSize: 50);

        var invalid = Assert.IsType<TransactionsResult.InvalidRequest>(result);
        Assert.Equal("filter", invalid.Field);
        Assert.Contains("not both", invalid.Message);
    }

    [Fact]
    public async Task AC_GLA_036_GetTransactionDetail_ReturnsFullJournalEntryWithLines()
    {
        // REQ-GLA-022, AC-GLA-036
        var result = await _handler.GetDetailAsync("1000-100-01", "je-000001");

        var success = Assert.IsType<TransactionsResult.DetailSuccess>(result);
        var detail = success.Detail;

        Assert.Equal("je-000001", detail.EntryId);
        Assert.Equal("Posted", detail.PostingStatus);
        Assert.NotEmpty(detail.Lines);
        Assert.Equal(2, detail.Lines.Count);
        Assert.Equal(1, detail.Lines[0].LineNumber);
        Assert.True(detail.Lines[0].Debit > 0 || detail.Lines[0].Credit > 0);
    }

    [Fact]
    public async Task AC_GLA_037_ExportTransactions_GeneratesCsvWithHeaderAndRows()
    {
        // REQ-GLA-023, AC-GLA-037
        var result = await _handler.ExportTransactionsAsync("1000-100-01", null, null, 2025, null);

        var export = Assert.IsType<TransactionsResult.ExportSuccess>(result);
        Assert.Equal("transactions-1000-100-01.csv", export.FileName);

        var csv = Encoding.UTF8.GetString(export.Content);
        Assert.StartsWith("entryId,batchId,description,amount,postingStatus,postingDate", csv);
        Assert.Contains("je-000001", csv);
    }

    [Fact]
    public async Task ListTransactions_NonExistentAccount_ReturnsNotFound()
    {
        var result = await _handler.ListTransactionsAsync("9999-999-99", null, null, null, null, 1, 50);

        var notFound = Assert.IsType<TransactionsResult.NotFound>(result);
        Assert.Equal("9999-999-99", notFound.Id);
    }
}
