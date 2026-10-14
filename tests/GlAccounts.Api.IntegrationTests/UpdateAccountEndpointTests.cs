using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.UpdateAccount;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlAccounts.Api.IntegrationTests;

public class UpdateAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UpdateAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AC_GLA_006_NFR_GLA_003_PutAccount_ValidUpdate_Returns200WithNewETag()
    {
        // REQ-GLA-006, AC-GLA-006, NFR-GLA-003
        var client = _factory.CreateClient();

        // 1. Get initial account and ETag
        var getResponse = await client.GetAsync("/api/gl/accounts/1000-100-01");
        var initialEtag = getResponse.Headers.ETag!.ToString();

        var updateRequest = new UpdateAccountRequest
        {
            Description = "Updated cash account",
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Put, "/api/gl/accounts/1000-100-01")
        {
            Content = JsonContent.Create(updateRequest)
        };
        requestMessage.Headers.Add("If-Match", initialEtag);
        requestMessage.Headers.Add("X-User-Id", "finance-admin");

        var start = DateTime.UtcNow;
        var response = await client.SendAsync(requestMessage);
        var elapsed = DateTime.UtcNow - start;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(elapsed.TotalSeconds < 1.0, "NFR-GLA-003: update <= 1 s");

        var newEtag = response.Headers.ETag?.ToString();
        Assert.NotNull(newEtag);
        Assert.NotEqual(initialEtag, newEtag);

        var account = await response.Content.ReadFromJsonAsync<Account>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(account);
        Assert.Equal("Updated cash account", account.Description);
    }

    [Fact]
    public async Task AC_GLA_056_PutAccount_StaleETag_Returns409Conflict()
    {
        // REQ-GLA-035, AC-GLA-056, D-04
        var client = _factory.CreateClient();

        var updateRequest = new UpdateAccountRequest
        {
            Description = "Conflicting update",
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Put, "/api/gl/accounts/1000-100-01")
        {
            Content = JsonContent.Create(updateRequest)
        };
        requestMessage.Headers.Add("If-Match", "\"stale-version-xyz\"");

        var response = await client.SendAsync(requestMessage);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Contains("changed by someone else", problem.Detail);
    }

    [Fact]
    public async Task AC_GLA_007_PutAccount_CategoryAndCurrency_AreReturnedAndReadBack()
    {
        // REQ-GLA-006, AC-GLA-007 (stub data only)
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();
        var get = await client.GetAsync("/api/gl/accounts/3000-100-01");
        var etag = get.Headers.ETag!.ToString();

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/gl/accounts/3000-100-01")
        {
            Content = JsonContent.Create(new { description = "Common stock", category = "Liability", currencyId = "GBP", status = "Active" })
        };
        request.Headers.Add("If-Match", etag);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var updated = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Liability", updated.RootElement.GetProperty("category").GetString());
        Assert.Equal("GBP", updated.RootElement.GetProperty("currencyId").GetString());

        var reread = await client.GetAsync("/api/gl/accounts/3000-100-01");
        using var doc = JsonDocument.Parse(await reread.Content.ReadAsStringAsync());
        Assert.Equal("Liability", doc.RootElement.GetProperty("category").GetString());
        Assert.Equal("GBP", doc.RootElement.GetProperty("currencyId").GetString());
    }

    [Fact]
    public async Task AC_GLA_009_PutAccount_InvalidCategoryAndCurrency_Returns400WithFieldErrors()
    {
        // REQ-GLA-007, AC-GLA-009 (stub data only)
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();
        var get = await client.GetAsync("/api/gl/accounts/1000-100-01");
        var etag = get.Headers.ETag!.ToString();

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/gl/accounts/1000-100-01")
        {
            Content = JsonContent.Create(new { description = "Should not be saved", category = "Statistical", currencyId = "XXX", status = "Active" })
        };
        request.Headers.Add("If-Match", etag);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("category", out _));
        Assert.True(errors.TryGetProperty("currencyId", out _));

        var reread = await client.GetAsync("/api/gl/accounts/1000-100-01");
        using var doc = JsonDocument.Parse(await reread.Content.ReadAsStringAsync());
        Assert.Equal("Cash and equivalents", doc.RootElement.GetProperty("description").GetString());
        Assert.Equal(etag, reread.Headers.ETag!.ToString());
    }

    [Fact]
    public async Task AC_GLA_010_PutAccount_UnknownAccount_Returns404()
    {
        // REQ-GLA-006, AC-GLA-010
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/gl/accounts/9999-999-99")
        {
            Content = JsonContent.Create(new { description = "Does not exist", category = "Asset", currencyId = "USD", status = "Active" })
        };
        request.Headers.Add("If-Match", "\"any-version\"");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutAccount_MissingIfMatch_Returns400()
    {
        var client = _factory.CreateClient();

        var updateRequest = new UpdateAccountRequest
        {
            Description = "No if-match update",
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active
        };

        var response = await client.PutAsJsonAsync("/api/gl/accounts/1000-100-01", updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
