using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.CreateAccount;
using GlAccounts.Api.Features.GlAccounts.StubData;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GlAccounts.Api.IntegrationTests;

public class ManageAttributesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ManageAttributesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AC_GLA_014_PostAttribute_Valid_Returns201WithLocation()
    {
        // REQ-GLA-009, AC-GLA-014
        var client = _factory.CreateClient();

        var store = _factory.Services.GetRequiredService<IAccountStore>();
        store.Reset();

        var input = new AttributeInput("project", "proj-alpha");

        var response = await client.PostAsJsonAsync("/api/gl/accounts/1000-100-01/attributes", input);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var attribute = await response.Content.ReadFromJsonAsync<AccountAttribute>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(attribute);
        Assert.Equal("project", attribute.AttributeTypeId);
        Assert.Equal("proj-alpha", attribute.AttributeValueId);
    }

    [Fact]
    public async Task AC_GLA_015_PutAttribute_Valid_Returns200WithUpdatedAttribute()
    {
        // REQ-GLA-009, AC-GLA-015
        var client = _factory.CreateClient();

        var input = new AttributeInput("dept", "sales");

        var response = await client.PutAsJsonAsync("/api/gl/accounts/1000-100-01/attributes/att-1", input);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var attribute = await response.Content.ReadFromJsonAsync<AccountAttribute>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(attribute);
        Assert.Equal("att-1", attribute.AttributeId);
        Assert.Equal("sales", attribute.AttributeValueId);
    }

    [Fact]
    public async Task AC_GLA_016_DeleteAttribute_Valid_Returns204NoContent()
    {
        // REQ-GLA-009, AC-GLA-016
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/gl/accounts/1000-100-01/attributes/att-2");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AC_GLA_014_AC_GLA_015_AC_GLA_016_NFR_GLA_004_StandaloneOperations_ReturnNewETagAndAreFast()
    {
        // REQ-GLA-009, REQ-GLA-031, NFR-GLA-004 (<= 1 s, stub data only)
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();
        var initial = (await client.GetAsync("/api/gl/accounts/1000-100-01")).Headers.ETag!.ToString();

        var start = DateTime.UtcNow;
        var post = await client.PostAsJsonAsync("/api/gl/accounts/1000-100-01/attributes", new AttributeInput("project", "proj-beta"));
        var elapsed = DateTime.UtcNow - start;
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Assert.True(elapsed.TotalSeconds < 1.0, "NFR-GLA-004: attribute operation <= 1 s");
        var afterPost = post.Headers.ETag!.ToString();
        Assert.NotEqual(initial, afterPost);

        var put = await client.PutAsJsonAsync("/api/gl/accounts/1000-100-01/attributes/att-1", new AttributeInput("dept", "sales"));
        var afterPut = put.Headers.ETag!.ToString();
        Assert.NotEqual(afterPost, afterPut);

        var delete = await client.DeleteAsync("/api/gl/accounts/1000-100-01/attributes/att-2");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var afterDelete = delete.Headers.ETag!.ToString();
        Assert.NotEqual(afterPut, afterDelete);

        var reread = await client.GetAsync("/api/gl/accounts/1000-100-01");
        Assert.Equal(afterDelete, reread.Headers.ETag!.ToString());
    }

    [Fact]
    public async Task AC_GLA_016_PostAttribute_ValueFromOtherType_Returns400FieldError()
    {
        // REQ-GLA-010, AC-GLA-016 (negative)
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/gl/accounts/1000-100-01/attributes", new AttributeInput("dept", "emea"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("attributeValueId", out _));
    }

    [Fact]
    public async Task AC_GLA_018_PutAccount_InvalidStagedChange_Returns400AndAccountUnchanged()
    {
        // REQ-GLA-011, AC-GLA-018 (negative, stub data only)
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();
        var get = await client.GetAsync("/api/gl/accounts/1000-100-01");
        var etag = get.Headers.ETag!.ToString();

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/gl/accounts/1000-100-01")
        {
            Content = JsonContent.Create(new
            {
                description = "Must not be saved",
                category = "Asset",
                currencyId = "USD",
                status = "Active",
                attributeChanges = new object[]
                {
                    new { action = "Add", attributeTypeId = "region", attributeValueId = "apac" },
                    new { action = "Add", attributeTypeId = "dept", attributeValueId = "not-in-catalog" }
                }
            })
        };
        request.Headers.Add("If-Match", etag);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var reread = await client.GetAsync("/api/gl/accounts/1000-100-01");
        Assert.Equal(etag, reread.Headers.ETag!.ToString());
        using var doc = JsonDocument.Parse(await reread.Content.ReadAsStringAsync());
        Assert.Equal("Cash and equivalents", doc.RootElement.GetProperty("description").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("attributes").GetArrayLength());
    }

    [Fact]
    public async Task AC_GLA_017_PutAccount_StagedChanges_AppliedTogether()
    {
        // REQ-GLA-011, AC-GLA-017 (stub data only)
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();
        var get = await client.GetAsync("/api/gl/accounts/1000-100-01");
        var etag = get.Headers.ETag!.ToString();

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/gl/accounts/1000-100-01")
        {
            Content = JsonContent.Create(new
            {
                description = "Updated with staged attributes",
                category = "Asset",
                currencyId = "USD",
                status = "Active",
                attributeChanges = new object[]
                {
                    new { action = "Add", attributeTypeId = "region", attributeValueId = "apac" },
                    new { action = "Remove", attributeId = "att-2" }
                }
            })
        };
        request.Headers.Add("If-Match", etag);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Updated with staged attributes", doc.RootElement.GetProperty("description").GetString());
        var attributes = doc.RootElement.GetProperty("attributes").EnumerateArray().ToList();
        Assert.Contains(attributes, a => a.GetProperty("attributeValueId").GetString() == "apac");
        Assert.DoesNotContain(attributes, a => a.GetProperty("attributeId").GetString() == "att-2");
    }

    [Fact]
    public async Task PostAttribute_InvalidCatalog_Returns400()
    {
        var client = _factory.CreateClient();

        var input = new AttributeInput("nonexistent", "invalid");

        var response = await client.PostAsJsonAsync("/api/gl/accounts/1000-100-01/attributes", input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
