using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlAccounts.Api.IntegrationTests;

// Setup smoke test (T001): proves the API host boots under WebApplicationFactory. Not tied to an acceptance criterion.
public class HostStartsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Host_starts_and_unknown_route_returns_404()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
