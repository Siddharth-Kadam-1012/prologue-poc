using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

/// <summary>
/// AC-GLA-012: GET /api/gl/accounts/{id}/transactions with optional date/status filters.
/// </summary>
public sealed class TransactionsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TransactionsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Transactions_AllRecords_Returns3()
    {
        // 01-200-2000 has 3 seeded transactions
        var response = await _client.GetAsync("/api/gl/accounts/01-200-2000/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionListDto>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be("01-200-2000");
        body.Total.Should().Be(3);
        body.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Get_Transactions_FilterFromDate_ReturnsSubset()
    {
        // txn-002 (2026-03-31) and txn-003 (2026-04-15) are on/after 2026-02-01
        var response = await _client.GetAsync("/api/gl/accounts/01-200-2000/transactions?fromDate=2026-02-01");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionListDto>();
        body!.Total.Should().Be(2);
    }

    [Fact]
    public async Task Get_Transactions_FilterToDate_ReturnsSubset()
    {
        // Only txn-001 (2026-01-31) is on/before 2026-02-01
        var response = await _client.GetAsync("/api/gl/accounts/01-200-2000/transactions?toDate=2026-02-01");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionListDto>();
        body!.Total.Should().Be(1);
    }

    [Fact]
    public async Task Get_Transactions_FilterPostingStatus_Posted_ReturnsAll3()
    {
        var response = await _client.GetAsync("/api/gl/accounts/01-200-2000/transactions?postingStatus=Posted");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionListDto>();
        body!.Total.Should().Be(3);
        body.Items.Should().OnlyContain(i => i.PostingStatus == "Posted");
    }

    [Fact]
    public async Task Get_Transactions_AccountWithNoTransactions_ReturnsEmpty()
    {
        // 01-100-1000 has no transactions
        var response = await _client.GetAsync("/api/gl/accounts/01-100-1000/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionListDto>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_Transactions_NonExistentAccount_Returns404()
    {
        var response = await _client.GetAsync("/api/gl/accounts/99-999-9999/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Transactions_InvalidFromDate_Returns400()
    {
        var response = await _client.GetAsync("/api/gl/accounts/01-200-2000/transactions?fromDate=not-a-date");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_Transactions_InvalidPostingStatus_Returns400()
    {
        var response = await _client.GetAsync("/api/gl/accounts/01-200-2000/transactions?postingStatus=INVALID");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record TransactionListDto(string AccountId, List<TransactionItemDto> Items, int Total);
    private sealed record TransactionItemDto(
        string TransactionId, string AccountId, string BatchId,
        string Description, decimal Amount, string PostingDate, string PostingStatus);
}
