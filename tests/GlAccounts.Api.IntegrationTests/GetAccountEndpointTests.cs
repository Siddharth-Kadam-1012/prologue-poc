using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlAccounts.Api.IntegrationTests;

public class GetAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AC_GLA_004_NFR_GLA_002_GetAccount_Existing_Returns200WithETagAndBody()
    {
        // REQ-GLA-005, AC-GLA-004, NFR-GLA-002
        var client = _factory.CreateClient();

        var start = DateTime.UtcNow;
        var response = await client.GetAsync("/api/gl/accounts/1000-100-01");
        var elapsed = DateTime.UtcNow - start;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(elapsed.TotalSeconds < 1.0, "NFR-GLA-002: read operation <= 1 s");

        Assert.NotNull(response.Headers.ETag);

        var account = await response.Content.ReadFromJsonAsync<Account>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(account);
        Assert.Equal("1000-100-01", account.AccountId);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.True(account.EligibleForPosting);
        Assert.Equal(3, account.Segments.Count);
        Assert.Equal(2, account.Attributes.Count);
    }

    [Fact]
    public async Task AC_GLA_005_GetAccount_NonExistent_Returns404ProblemDetails()
    {
        // REQ-GLA-005, AC-GLA-005
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gl/accounts/9999-999-99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Account '9999-999-99' was not found.", problem.Detail);
    }

    [Theory]
    [InlineData("1000-100-01", true)]
    [InlineData("4000-100-01", false)]
    [InlineData("5000-100-01", false)]
    [InlineData("9000-100-01", false)]
    public async Task AC_GLA_020_AC_GLA_021_GetAccount_EligibilityAcrossStatuses(string accountId, bool expectedEligible)
    {
        // REQ-GLA-013, AC-GLA-020, AC-GLA-021
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/gl/accounts/{accountId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var account = await response.Content.ReadFromJsonAsync<Account>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(account);
        Assert.Equal(expectedEligible, account.EligibleForPosting);
    }

    [Fact]
    public async Task GetAccount_IdLongerThan32Chars_Returns400()
    {
        var client = _factory.CreateClient();
        var longId = "1000-100-01-" + new string('9', 30);

        var response = await client.GetAsync($"/api/gl/accounts/{longId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
