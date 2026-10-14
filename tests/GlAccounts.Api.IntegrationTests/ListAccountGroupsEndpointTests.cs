using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlAccounts.Api.Common;
using GlAccounts.Api.Features.GlAccounts.ListAccountGroups;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlAccounts.Api.IntegrationTests;

public class ListAccountGroupsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ListAccountGroupsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AC_GLA_024_GetGroups_ExistingAccount_Returns200WithGroupList()
    {
        // REQ-GLA-015, AC-GLA-024
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/1000-100-01/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<AccountGroupListResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(list);
        Assert.NotEmpty(list.Items);
        Assert.Contains(list.Items, g => g.GroupId == "grp-revenue-na");
    }

    [Fact]
    public async Task AC_GLA_027_ExportGroups_Returns200WithCsv()
    {
        // REQ-GLA-016, AC-GLA-027
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/1000-100-01/groups/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("account-groups-1000-100-01.csv", response.Content.Headers.ContentDisposition?.FileName);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("groupId,name,parentChain", csv);
        Assert.Contains("grp-revenue-na", csv);
    }

    [Fact]
    public async Task AC_GLA_025_GetGroups_AccountInNoGroups_Returns200WithEmptyItems()
    {
        // REQ-GLA-015, AC-GLA-025 (stub data only)
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/8000-100-01/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task AC_GLA_024_GetGroups_NestedGroup_ReturnsParentChainNearestFirst()
    {
        // REQ-GLA-015, AC-GLA-024
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/1000-100-01/groups");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var group = doc.RootElement.GetProperty("items").EnumerateArray()
            .Single(g => g.GetProperty("groupId").GetString() == "grp-revenue-na");
        var chain = group.GetProperty("parentChain").EnumerateArray().Select(p => p.GetProperty("groupId").GetString()).ToArray();
        Assert.Equal(new[] { "grp-revenue", "grp-all" }, chain);
    }

    [Fact]
    public async Task AC_GLA_027_ExportGroups_AccountInNoGroups_ReturnsOnlyHeader()
    {
        // REQ-GLA-016, AC-GLA-027
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/8000-100-01/groups/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("groupId,name,parentChain", (await response.Content.ReadAsStringAsync()).Trim());
    }

    [Fact]
    public async Task AC_GLA_027_ExportGroups_NonExistent_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/9999-999-99/groups/export");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetGroups_NonExistent_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/9999-999-99/groups");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
