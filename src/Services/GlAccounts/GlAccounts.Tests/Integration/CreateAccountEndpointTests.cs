using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

public sealed class CreateAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CreateAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── AC-GLA-001: valid account → 201 ─────────────────────────────────────

    [Fact]
    public async Task Post_ValidAccount_Returns201WithBody()
    {
        var payload = new
        {
            accountId     = "01-300-1000",
            description   = "New Cost Centre",
            accountType   = "Financial",
            category      = "Asset",
            status        = "Active",
            balanceType   = "BalanceForward",
            normalBalance = "Debit",
            currencyId    = "USD"
        };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Contain("01-300-1000");

        var body = await response.Content.ReadFromJsonAsync<AccountResponseDto>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be("01-300-1000");
        body.Description.Should().Be("New Cost Centre");
        body.AccountType.Should().Be("Financial");
        body.Category.Should().Be("Asset");
        body.Status.Should().Be("Active");
        body.CreatedBy.Should().Be("anonymous");   // no X-User-Id header sent
    }

    [Fact]
    public async Task Post_ValidAccount_WithUserIdHeader_SetsCreatedBy()
    {
        var payload = new
        {
            accountId     = "01-300-2000",
            description   = "Test with user",
            accountType   = "Financial",
            category      = "Liability",
            status        = "Active",
            balanceType   = "Periodic",
            normalBalance = "Credit"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/gl/accounts");
        request.Content = JsonContent.Create(payload);
        request.Headers.Add("X-User-Id", "jsmith");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AccountResponseDto>();
        body!.CreatedBy.Should().Be("jsmith");
    }

    // ── AC-GLA-002: duplicate account ID → 409 ───────────────────────────────

    [Fact]
    public async Task Post_DuplicateAccountId_Returns409()
    {
        // 01-100-1000 is seeded — should always conflict
        var payload = new
        {
            accountId     = "01-100-1000",
            description   = "Duplicate",
            accountType   = "Financial",
            category      = "Asset",
            status        = "Active",
            balanceType   = "BalanceForward",
            normalBalance = "Debit"
        };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("already exists");
    }

    // ── AC-GLA-003: bad segment format → 422 ────────────────────────────────

    [Fact]
    public async Task Post_InvalidSegmentFormat_Returns422()
    {
        var payload = new
        {
            accountId     = "INVALID",
            description   = "Bad format",
            accountType   = "Financial",
            category      = "Asset",
            status        = "Active",
            balanceType   = "BalanceForward",
            normalBalance = "Debit"
        };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("segment format");
    }

    [Fact]
    public async Task Post_InvalidCategoryForType_Returns422()
    {
        var payload = new
        {
            accountId     = "01-300-9999",
            description   = "Bad category",
            accountType   = "Statistical",   // Statistical account
            category      = "Asset",          // Asset is only valid for Financial
            status        = "Active",
            balanceType   = "BalanceForward",
            normalBalance = "Debit"
        };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not valid for account type");
    }

    [Fact]
    public async Task Post_MissingRequiredFields_Returns400Or422()
    {
        var payload = new { accountId = "01-300-8888" }; // missing most fields

        var response = await _client.PostAsJsonAsync("/api/gl/accounts", payload);

        // FluentValidation returns 422; model binding may return 400
        ((int)response.StatusCode).Should().BeOneOf(400, 422);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private sealed record AccountResponseDto(
        string AccountId,
        string Description,
        string AccountType,
        string Category,
        string Status,
        string BalanceType,
        string NormalBalance,
        string? CurrencyId,
        string CreatedDate,
        string CreatedBy);
}
