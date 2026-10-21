using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

/// <summary>
/// AC-GLA-017: POST /api/gl/accounts/{sourceId}/copy — copy settings from a Template account.
/// </summary>
public sealed class CopyFromTemplateEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CopyFromTemplateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── AC-GLA-017: successful copy ───────────────────────────────────────────

    [Fact]
    public async Task Post_CopyTemplate_Returns201WithCopiedFields()
    {
        // 01-100-5000 is seeded as Template (Financial, Asset, BalanceForward, Debit)
        var payload = new { newAccountId = "01-300-2201", description = "Copy of Template Account" };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts/01-100-5000/copy", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Contain("01-300-2201");

        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be("01-300-2201");
        body.Description.Should().Be("Copy of Template Account");
        body.AccountType.Should().Be("Financial");      // copied from source
        body.Category.Should().Be("Asset");             // copied from source
        body.Status.Should().Be("Active");              // copies always Active
        body.BalanceType.Should().Be("BalanceForward"); // copied
        body.NormalBalance.Should().Be("Debit");        // copied
    }

    [Fact]
    public async Task Post_CopyTemplate_SourceRemainsTemplate()
    {
        // Source account must still be Template after the copy
        await _client.PostAsJsonAsync("/api/gl/accounts/01-100-5000/copy",
            new { newAccountId = "01-300-2202", description = "Another copy" });

        var sourceResponse = await _client.GetAsync("/api/gl/accounts/01-100-5000");
        sourceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var source = await sourceResponse.Content.ReadFromJsonAsync<AccountDto>();
        source!.Status.Should().Be("Template");
    }

    // ── 422: source not a Template ────────────────────────────────────────────

    [Fact]
    public async Task Post_CopyNonTemplateAccount_Returns422()
    {
        // 01-100-1000 is Active, not Template
        var payload = new { newAccountId = "01-300-2203", description = "Should fail" };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts/01-100-1000/copy", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not a Template account");
    }

    [Fact]
    public async Task Post_Copy_NewIdInvalidFormat_Returns422()
    {
        var payload = new { newAccountId = "INVALID", description = "Bad format" };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts/01-100-5000/copy", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("segment format");
    }

    // ── 409: duplicate new account ID ────────────────────────────────────────

    [Fact]
    public async Task Post_Copy_DuplicateNewId_Returns409()
    {
        // 01-100-1000 is already seeded — use it as the new ID to force a conflict
        var payload = new { newAccountId = "01-100-1000", description = "Conflict copy" };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts/01-100-5000/copy", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("already exists");
    }

    // ── 404: source not found ─────────────────────────────────────────────────

    [Fact]
    public async Task Post_Copy_SourceNotFound_Returns404()
    {
        var payload = new { newAccountId = "01-300-2299", description = "Missing source" };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts/99-999-9999/copy", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record AccountDto(
        string AccountId, string Description, string AccountType,
        string Category, string Status, string BalanceType, string NormalBalance,
        string? CurrencyId, string CreatedDate, string CreatedBy);
}
