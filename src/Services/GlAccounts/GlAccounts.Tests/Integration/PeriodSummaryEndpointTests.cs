using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

/// <summary>
/// AC-GLA-011: GET /api/gl/accounts/{id}/period-summary with optional filters.
/// </summary>
public sealed class PeriodSummaryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PeriodSummaryEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_PeriodSummary_AllPeriods_Returns4Records()
    {
        // 01-100-1000 has 4 seed period-balance records (FY2026 P1–P4)
        var response = await _client.GetAsync("/api/gl/accounts/01-100-1000/period-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PeriodSummaryDto>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be("01-100-1000");
        body.Total.Should().Be(4);
        body.Items.Should().HaveCount(4);
    }

    [Fact]
    public async Task Get_PeriodSummary_FilterFiscalYear_Returns4Records()
    {
        var response = await _client.GetAsync("/api/gl/accounts/01-100-1000/period-summary?fiscalYear=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PeriodSummaryDto>();
        body!.Total.Should().Be(4);
        body.Items.Should().OnlyContain(i => i.FiscalYear == 2026);
    }

    [Fact]
    public async Task Get_PeriodSummary_FilterPeriodRange_ReturnsSubset()
    {
        var response = await _client.GetAsync("/api/gl/accounts/01-100-1000/period-summary?fromPeriod=2&toPeriod=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PeriodSummaryDto>();
        body!.Total.Should().Be(2);
        body.Items.Should().OnlyContain(i => i.FiscalPeriod >= 2 && i.FiscalPeriod <= 3);
    }

    [Fact]
    public async Task Get_PeriodSummary_AccountWithNoBalances_ReturnsEmpty()
    {
        // 01-200-1000 has no seed period-balance records
        var response = await _client.GetAsync("/api/gl/accounts/01-200-1000/period-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PeriodSummaryDto>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_PeriodSummary_NonExistentAccount_Returns404()
    {
        var response = await _client.GetAsync("/api/gl/accounts/99-999-9999/period-summary");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record PeriodSummaryDto(string AccountId, List<PeriodBalanceItemDto> Items, int Total);
    private sealed record PeriodBalanceItemDto(string BalanceId, string AccountId, int FiscalYear, int FiscalPeriod, string? BudgetVersion, decimal Balance);
}
