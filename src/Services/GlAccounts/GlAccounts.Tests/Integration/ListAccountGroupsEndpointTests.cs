using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GlAccounts.Tests.Integration;

/// <summary>
/// AC-GLA-010: GET /api/gl/accounts/{id}/groups returns correct groups.
/// </summary>
public sealed class ListAccountGroupsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ListAccountGroupsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_AccountInGroup_ReturnsGroupList()
    {
        // 01-100-1000 is in GRP-001 per seed data
        var response = await _client.GetAsync("/api/gl/accounts/01-100-1000/groups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GroupListDto>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be("01-100-1000");
        body.Total.Should().Be(1);
        body.Items.Should().HaveCount(1);
        body.Items[0].GroupId.Should().Be("GRP-001");
        body.Items[0].GroupName.Should().Be("Current Assets");
    }

    [Fact]
    public async Task Get_AccountNotInAnyGroup_ReturnsEmptyList()
    {
        // 01-200-1000 (Accounts Payable) is not in any group
        var response = await _client.GetAsync("/api/gl/accounts/01-200-1000/groups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GroupListDto>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_NonExistentAccount_Returns404()
    {
        var response = await _client.GetAsync("/api/gl/accounts/99-999-9999/groups");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record GroupListDto(string AccountId, List<GroupItemDto> Items, int Total);
    private sealed record GroupItemDto(string GroupId, string GroupName, string? Description);
}
