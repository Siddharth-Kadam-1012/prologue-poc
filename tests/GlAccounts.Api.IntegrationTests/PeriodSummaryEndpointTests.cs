using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlAccounts.Api.Common;
using GlAccounts.Api.Features.GlAccounts.StubData;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlAccounts.Api.IntegrationTests;

public class PeriodSummaryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PeriodSummaryEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AC_GLA_028_NFR_GLA_004_GetPeriodSummary_Returns200WithPeriodSummaryJson()
    {
        // REQ-GLA-017, AC-GLA-028, NFR-GLA-004
        var client = _factory.CreateClient();

        var start = DateTime.UtcNow;
        var response = await client.GetAsync("/api/gl/accounts/1000-100-01/period-summary");
        var elapsed = DateTime.UtcNow - start;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(elapsed.TotalSeconds < 1.0, "NFR-GLA-004: period summary <= 1 s");

        var summary = await response.Content.ReadFromJsonAsync<PeriodSummary>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(summary);
        Assert.Equal("1000-100-01", summary.AccountId);
        Assert.NotEmpty(summary.Periods);
        Assert.NotEmpty(summary.Years);
        Assert.NotEmpty(summary.BudgetVersions);
    }

    [Fact]
    public async Task AC_GLA_032_ExportPeriodSummary_ReturnsExcelFileWithContentDisposition()
    {
        // REQ-GLA-019, AC-GLA-032
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/1000-100-01/period-summary/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("period-summary-1000-100-01.xlsx", response.Content.Headers.ContentDisposition?.FileName);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task GetPeriodSummary_NonExistent_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/9999-999-99/period-summary");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
