using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

public sealed class GetAndUpdateAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GetAndUpdateAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ExistingAccount_Returns200WithBody()
    {
        // Use 01-200-1000 (Accounts Payable) — not mutated by any PUT test
        var response = await _client.GetAsync("/api/gl/accounts/01-200-1000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be("01-200-1000");
        body.Description.Should().Be("Accounts Payable");
        body.AccountType.Should().Be("Financial");
        body.Category.Should().Be("Liability");
        body.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Get_NonExistentAccount_Returns404()
    {
        var response = await _client.GetAsync("/api/gl/accounts/99-999-9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT — AC-GLA-005: update editable field → 200 ────────────────────────

    [Fact]
    public async Task Put_UpdateDescription_Returns200WithUpdatedBody()
    {
        // First create a fresh account so mutations don't bleed across tests
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "01-300-5501", description = "PUT Test Account",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        var payload = new { description = "Updated Description" };

        var response = await _client.PutAsJsonAsync("/api/gl/accounts/01-300-5501", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body!.Description.Should().Be("Updated Description");
        body.AccountId.Should().Be("01-300-5501");  // unchanged
        body.AccountType.Should().Be("Financial");   // permanent, unchanged
    }

    [Fact]
    public async Task Put_UpdateStatus_Returns200()
    {
        // Create a dedicated account for this test
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "01-300-5502", description = "Status Test Account",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        var payload = new { status = "Hold" };

        var response = await _client.PutAsJsonAsync("/api/gl/accounts/01-300-5502", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body!.Status.Should().Be("Hold");
    }

    [Fact]
    public async Task Put_NonExistentAccount_Returns404()
    {
        var payload = new { description = "Ghost" };

        var response = await _client.PutAsJsonAsync("/api/gl/accounts/99-999-9999", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT — AC-GLA-004: attempt to change permanent field → 422 ────────────

    [Fact]
    public async Task Put_AttemptToChangeAccountType_Returns422()
    {
        // Sending accountType in body signals a permanent-field change attempt
        var payload = new { accountType = "Statistical" };

        var response = await _client.PutAsJsonAsync("/api/gl/accounts/01-200-1000", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("AccountType cannot be changed");
    }

    [Fact]
    public async Task Put_AttemptToChangeAccountId_Returns422()
    {
        var payload = new { accountId = "99-999-9999" };

        var response = await _client.PutAsJsonAsync("/api/gl/accounts/01-200-1000", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("AccountId cannot be changed");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private sealed record AccountDto(
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
