using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlAccounts.Api.Features.GlAccounts.Templates;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlAccounts.Api.IntegrationTests;

// Stub data only: T011 Templates (API-GLA-015, API-GLA-016).
// Covers AC-GLA-053 (create from template), AC-GLA-054 (non-template source), AC-GLA-055 (search templates).
public class TemplatesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly WebApplicationFactory<Program> _factory;

    public TemplatesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // Fresh seeded store per test: each test gets its own host.
    private HttpClient NewClient() => _factory.WithWebHostBuilder(_ => { }).CreateClient();

    [Fact]
    public async Task AC_GLA_055_API_GLA_015_SearchTemplates_ReturnsOnlyMatchingTemplates()
    {
        var client = NewClient();

        var response = await client.GetAsync("/api/gl/accounts?status=Template&q=standard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<AccountSummaryPage>(Json);
        Assert.NotNull(page);
        var item = Assert.Single(page.Items);
        Assert.Equal("9000-100-01", item.AccountId);
        Assert.Equal(1, page.Total);
        Assert.Equal(1, page.Page);
        Assert.Equal(50, page.PageSize);
    }

    [Fact]
    public async Task AC_GLA_055_API_GLA_015_Search_PageSizeOver200_Returns400()
    {
        var client = NewClient();

        var response = await client.GetAsync("/api/gl/accounts?pageSize=201");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AC_GLA_055_API_GLA_015_Search_InvalidStatus_Returns400()
    {
        var client = NewClient();

        var response = await client.GetAsync("/api/gl/accounts?status=Bogus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AC_GLA_053_API_GLA_016_CreateFromTemplate_Returns201AndTemplateUnchanged()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "6000-100-01",
            templateAccountId = "9000-100-01"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/gl/accounts/6000-100-01", response.Headers.Location?.ToString());
        Assert.NotNull(response.Headers.ETag);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Standard expense template", created.RootElement.GetProperty("description").GetString());
        Assert.Equal("Expense", created.RootElement.GetProperty("category").GetString());
        Assert.Equal("Active", created.RootElement.GetProperty("status").GetString());
        Assert.True(created.RootElement.GetProperty("eligibleForPosting").GetBoolean());

        var template = await client.GetAsync("/api/gl/accounts/9000-100-01");
        using var templateDoc = JsonDocument.Parse(await template.Content.ReadAsStringAsync());
        Assert.Equal("Template", templateDoc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AC_GLA_054_API_GLA_016_CreateFromNonTemplate_Returns400NamingNotATemplate()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "6000-100-01",
            templateAccountId = "1000-100-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("not a template", body);
    }

    [Fact]
    public async Task AC_GLA_053_API_GLA_016_CreateFromUnknownTemplate_Returns404()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "6000-100-01",
            templateAccountId = "9999-999-99"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Template account '9999-999-99' was not found.", body);
    }
}
