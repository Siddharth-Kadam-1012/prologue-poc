using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GlAccounts.Domain.Services;
using GlAccounts.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

/// <summary>
/// AC-GLA-007: Account status persists; StatusRules enforced.
/// </summary>
public sealed class AccountStatusTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AccountStatusTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── StatusRules unit assertions (T047) ───────────────────────────────────

    [Theory]
    [InlineData(AccountStatus.Active,   true)]
    [InlineData(AccountStatus.Hold,     false)]
    [InlineData(AccountStatus.Inactive, false)]
    [InlineData(AccountStatus.Template, false)]
    public void StatusRules_CanPostTransactions_CorrectForEachStatus(AccountStatus status, bool expected)
    {
        StatusRules.CanPostTransactions(status).Should().Be(expected);
    }

    [Theory]
    [InlineData(AccountStatus.Active,   AccountStatus.Hold,     true)]
    [InlineData(AccountStatus.Active,   AccountStatus.Inactive, true)]
    [InlineData(AccountStatus.Active,   AccountStatus.Template, true)]
    [InlineData(AccountStatus.Hold,     AccountStatus.Active,   true)]
    [InlineData(AccountStatus.Hold,     AccountStatus.Inactive, true)]
    [InlineData(AccountStatus.Inactive, AccountStatus.Active,   true)]
    [InlineData(AccountStatus.Template, AccountStatus.Active,   true)]
    [InlineData(AccountStatus.Hold,     AccountStatus.Template, false)]
    [InlineData(AccountStatus.Inactive, AccountStatus.Hold,     false)]
    [InlineData(AccountStatus.Template, AccountStatus.Inactive, false)]
    public void StatusRules_Transitions_AreCorrect(AccountStatus from, AccountStatus to, bool expected)
    {
        StatusRules.IsValidTransition(from, to).Should().Be(expected);
    }

    // ── AC-GLA-007: seeded Inactive account is returned as Inactive ───────────

    [Fact]
    public async Task Get_InactiveAccount_ReturnsStatusInactive()
    {
        // 01-100-3000 is seeded as Inactive
        var response = await _client.GetAsync("/api/gl/accounts/01-100-3000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body!.Status.Should().Be("Inactive");
    }

    [Fact]
    public async Task Get_HoldAccount_ReturnsStatusHold()
    {
        // 01-100-4000 is seeded as Hold
        var response = await _client.GetAsync("/api/gl/accounts/01-100-4000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body!.Status.Should().Be("Hold");
    }

    [Fact]
    public async Task Get_TemplateAccount_ReturnsStatusTemplate()
    {
        // 01-100-5000 is seeded as Template
        var response = await _client.GetAsync("/api/gl/accounts/01-100-5000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body!.Status.Should().Be("Template");
    }

    [Fact]
    public async Task Put_ChangeStatus_ActiveToHold_Returns200()
    {
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "01-300-8001", description = "Status lifecycle test",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        var response = await _client.PutAsJsonAsync("/api/gl/accounts/01-300-8001", new { status = "Hold" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body!.Status.Should().Be("Hold");
    }

    [Fact]
    public async Task Put_ChangeStatus_HoldToActive_Returns200()
    {
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "01-300-8002", description = "Hold to Active test",
            accountType = "Financial", category = "Asset",
            status = "Hold", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        var response = await _client.PutAsJsonAsync("/api/gl/accounts/01-300-8002", new { status = "Active" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body!.Status.Should().Be("Active");
    }

    private sealed record AccountDto(string AccountId, string Status);
}
