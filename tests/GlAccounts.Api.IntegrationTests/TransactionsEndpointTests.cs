using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlAccounts.Api.Common;
using GlAccounts.Api.Features.GlAccounts.StubData;
using GlAccounts.Api.Features.GlAccounts.Transactions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlAccounts.Api.IntegrationTests;

public class TransactionsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TransactionsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AC_GLA_033_NFR_GLA_004_GetTransactions_PagedList_Returns200WithTransactions()
    {
        // REQ-GLA-020, AC-GLA-033, NFR-GLA-004
        var client = _factory.CreateClient();

        var start = DateTime.UtcNow;
        var response = await client.GetAsync("/api/gl/accounts/1000-200-01/transactions?page=1&pageSize=50");
        var elapsed = DateTime.UtcNow - start;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(elapsed.TotalSeconds < 1.0, "NFR-GLA-004: transaction list <= 1 s");

        var page = await response.Content.ReadFromJsonAsync<JournalEntryPage>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(page);
        Assert.Equal(10001, page.Total);
        Assert.Equal(50, page.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(50, page.PageSize);
    }

    [Fact]
    public async Task AC_GLA_035_GetTransactions_MutualExclusivityFilter_Returns400()
    {
        // REQ-GLA-021, AC-GLA-035
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/1000-100-01/transactions?fromDate=2025-01-01&fiscalYear=2025");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AC_GLA_036_GetTransactionDetail_Returns200WithLines()
    {
        // REQ-GLA-022, AC-GLA-036
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/1000-100-01/transactions/je-000001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await response.Content.ReadFromJsonAsync<JournalEntryDetail>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(detail);
        Assert.Equal("je-000001", detail.EntryId);
        Assert.NotEmpty(detail.Lines);
    }

    [Fact]
    public async Task AC_GLA_037_ExportTransactions_Returns200WithCsv()
    {
        // REQ-GLA-023, AC-GLA-037
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/1000-100-01/transactions/export?fiscalYear=2025");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("transactions-1000-100-01.csv", response.Content.Headers.ContentDisposition?.FileName);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("entryId,batchId,description,amount,postingStatus,postingDate", csv);
    }
}
