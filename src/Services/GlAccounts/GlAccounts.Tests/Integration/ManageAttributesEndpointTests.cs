using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

/// <summary>
/// AC-GLA-008: POST attribute → 201; catalog validation.
/// AC-GLA-009: Sequence add → update → delete commits atomically.
/// </summary>
public sealed class ManageAttributesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ManageAttributesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── AC-GLA-008: POST → 201 ───────────────────────────────────────────────

    [Fact]
    public async Task Post_ValidAttribute_Returns201()
    {
        // 01-100-2000 exists in seed data; COST-CENTRE is seeded in catalog
        var payload = new { attributeTypeId = "COST-CENTRE", attributeValueId = "CC-200" };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts/01-100-2000/attributes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AttributeDto>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be("01-100-2000");
        body.AttributeTypeId.Should().Be("COST-CENTRE");
        body.AttributeValueId.Should().Be("CC-200");
        body.AttributeId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Post_UnknownAttributeType_Returns422()
    {
        var payload = new { attributeTypeId = "UNKNOWN-TYPE", attributeValueId = "X" };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts/01-100-2000/attributes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("does not exist in the catalog");
    }

    [Fact]
    public async Task Post_InvalidAttributeValue_Returns422()
    {
        var payload = new { attributeTypeId = "COST-CENTRE", attributeValueId = "CC-INVALID" };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts/01-100-2000/attributes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not valid for type");
    }

    [Fact]
    public async Task Post_AccountNotFound_Returns404()
    {
        var payload = new { attributeTypeId = "COST-CENTRE", attributeValueId = "CC-100" };

        var response = await _client.PostAsJsonAsync("/api/gl/accounts/99-999-9999/attributes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── AC-GLA-009: atomic add → update → delete sequence ────────────────────

    [Fact]
    public async Task AddUpdateRemove_Sequence_CommitsAtomically()
    {
        // Create a dedicated account for this test
        var accountId = "01-300-7701";
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId, description = "Attributes atomic test",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        // 1. ADD
        var addResponse = await _client.PostAsJsonAsync(
            $"/api/gl/accounts/{accountId}/attributes",
            new { attributeTypeId = "COST-CENTRE", attributeValueId = "CC-100" });
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var added = await addResponse.Content.ReadFromJsonAsync<AttributeDto>();
        var attributeId = added!.AttributeId;

        // 2. UPDATE to a different value
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/gl/accounts/{accountId}/attributes/{attributeId}",
            new { attributeValueId = "CC-300" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AttributeDto>();
        updated!.AttributeValueId.Should().Be("CC-300");

        // 3. DELETE
        var deleteResponse = await _client.DeleteAsync(
            $"/api/gl/accounts/{accountId}/attributes/{attributeId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateAttribute_InvalidValue_Returns422()
    {
        var accountId = "01-300-7702";
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId, description = "Attr update invalid test",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        var addResponse = await _client.PostAsJsonAsync(
            $"/api/gl/accounts/{accountId}/attributes",
            new { attributeTypeId = "COST-CENTRE", attributeValueId = "CC-100" });
        var added = await addResponse.Content.ReadFromJsonAsync<AttributeDto>();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/gl/accounts/{accountId}/attributes/{added!.AttributeId}",
            new { attributeValueId = "CC-BOGUS" });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteAttribute_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync(
            "/api/gl/accounts/01-100-1000/attributes/non-existent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record AttributeDto(
        string AttributeId,
        string AccountId,
        string AttributeTypeId,
        string AttributeTypeDescription,
        string AttributeValueId,
        string AttributeValueDescription);
}
