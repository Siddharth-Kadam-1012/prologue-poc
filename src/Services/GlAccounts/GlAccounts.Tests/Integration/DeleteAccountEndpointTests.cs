using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

/// <summary>
/// AC-GLA-013: Account with transactions → DELETE → 409 with count + deactivation suggestion.
/// AC-GLA-014: Clean account → DELETE → 204.
/// </summary>
public sealed class DeleteAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DeleteAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── AC-GLA-013: account with transactions → 409 ───────────────────────────

    [Fact]
    public async Task Delete_AccountWithTransactions_Returns409WithDetails()
    {
        // 01-200-2000 is seeded with 3 posted transactions
        var response = await _client.DeleteAsync("/api/gl/accounts/01-200-2000");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("3 transaction(s)");
        body.Should().Contain("Inactive");
    }

    // ── AC-GLA-014: clean account → 204 ──────────────────────────────────────

    [Fact]
    public async Task Delete_CleanAccount_Returns204()
    {
        // Create a fresh account with no dependencies
        var accountId = "01-300-9901";
        var createResponse = await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId, description = "Delete test account",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.DeleteAsync($"/api/gl/accounts/{accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistentAccount_Returns404()
    {
        var response = await _client.DeleteAsync("/api/gl/accounts/99-999-9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AlreadyDeletedAccount_Returns404()
    {
        // Create and delete, then try to delete again
        var accountId = "01-300-9902";
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId, description = "Double delete test",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        await _client.DeleteAsync($"/api/gl/accounts/{accountId}");
        var response = await _client.DeleteAsync($"/api/gl/accounts/{accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
