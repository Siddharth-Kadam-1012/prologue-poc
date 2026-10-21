using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

/// <summary>
/// AC-GLA-006: Category must be valid for AccountType on both CREATE and UPDATE.
/// </summary>
public sealed class AccountClassificationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AccountClassificationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Asset")]
    [InlineData("Liability")]
    [InlineData("Equity")]
    [InlineData("Revenue")]
    [InlineData("Expense")]
    public async Task Post_Financial_WithValidCategory_Returns201(string category)
    {
        // Use a unique ID per category to avoid cross-test collisions
        var id = $"01-300-{6000 + Array.IndexOf(["Asset","Liability","Equity","Revenue","Expense"], category)}";
        var payload = new
        {
            accountId = id, description = $"Financial {category} account",
            accountType = "Financial", category,
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("Headcount")]
    [InlineData("Units")]
    [InlineData("Area")]
    [InlineData("Other")]
    public async Task Post_Statistical_WithValidCategory_Returns201(string category)
    {
        var id = $"01-300-{7000 + Array.IndexOf(["Headcount","Units","Area","Other"], category)}";
        var payload = new
        {
            accountId = id, description = $"Statistical {category} account",
            accountType = "Statistical", category,
            status = "Active", balanceType = "Periodic", normalBalance = "Debit"
        };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_Statistical_WithFinancialCategory_Returns422()
    {
        // AC-GLA-006: Statistical + Asset → 422
        var payload = new
        {
            accountId = "01-300-9001", description = "Bad category",
            accountType = "Statistical", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not valid for account type");
    }

    [Fact]
    public async Task Post_Financial_WithStatisticalCategory_Returns422()
    {
        var payload = new
        {
            accountId = "01-300-9002", description = "Bad category",
            accountType = "Financial", category = "Headcount",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── UPDATE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_UpdateToValidCategory_Returns200()
    {
        // Create a Financial account, then change category to another valid one
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "01-300-9003", description = "Reclassify test",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        var response = await _client.PutAsJsonAsync("/api/gl/accounts/01-300-9003", new { category = "Expense" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body!.Category.Should().Be("Expense");
    }

    [Fact]
    public async Task Put_UpdateToInvalidCategory_Returns422()
    {
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "01-300-9004", description = "Bad update category",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        var response = await _client.PutAsJsonAsync("/api/gl/accounts/01-300-9004", new { category = "Headcount" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not valid for account type");
    }

    private sealed record AccountDto(string AccountId, string Category, string AccountType);
}
