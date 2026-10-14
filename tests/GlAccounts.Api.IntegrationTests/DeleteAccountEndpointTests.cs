using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlAccounts.Api.Common;
using GlAccounts.Api.Features.GlAccounts.StubData;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GlAccounts.Api.IntegrationTests;

public class DeleteAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DeleteAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AC_GLA_041_DeleteAccount_NoDependencies_Returns204AndSubsequentGet404()
    {
        // REQ-GLA-024, AC-GLA-041
        var client = _factory.CreateClient();

        // Ensure store has 1000-100-03
        var store = _factory.Services.GetRequiredService<IAccountStore>();
        store.Reset();

        var deleteResponse = await client.DeleteAsync("/api/gl/accounts/1000-100-03");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/gl/accounts/1000-100-03");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task AC_GLA_038_NFR_GLA_005_DeleteAccount_Blocked_Returns409DeleteBlockedProblemDetails()
    {
        // REQ-GLA-024, REQ-GLA-026, AC-GLA-038, NFR-GLA-005
        var client = _factory.CreateClient();

        var start = DateTime.UtcNow;
        var response = await client.DeleteAsync("/api/gl/accounts/1000-200-01");
        var elapsed = DateTime.UtcNow - start;

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(elapsed.TotalSeconds < 1.0, "NFR-GLA-005: delete dependency check <= 1 s");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<DeleteBlockedProblemDetails>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Contains("10001 posted transaction(s)", problem.Detail);
        Assert.NotEmpty(problem.Blockers);
        Assert.Contains(problem.Blockers, b => b.Kind == "PostedTransactions" && b.Count == 10001);
        Assert.Contains("Inactive", problem.Suggestion);
    }

    [Fact]
    public async Task DeleteAccount_NonExistent_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/gl/accounts/9999-999-99");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
