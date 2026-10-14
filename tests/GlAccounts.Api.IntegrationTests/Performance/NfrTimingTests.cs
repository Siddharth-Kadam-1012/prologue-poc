using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlAccounts.Api.IntegrationTests.Performance;

// T013: NFR timing checks. Stub data only: the in-memory store is not a real database, so these
// numbers say nothing about production latency. Ceilings are the NFR limits; each test warms the
// host up first so one-time startup cost is not counted. No fixed sleeps.
public class NfrTimingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly TimeSpan CreateCeiling = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan OperationCeiling = TimeSpan.FromSeconds(1);

    private readonly WebApplicationFactory<Program> _factory;

    public NfrTimingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> WarmClientAsync()
    {
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();
        (await client.GetAsync("/api/gl/accounts/1000-100-01")).EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task NFR_GLA_001_CreateAccount_CompletesWithin2Seconds_StubDataOnly()
    {
        var client = await WarmClientAsync();

        var watch = Stopwatch.StartNew();
        var response = await client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "1000-100-02",
            description = "Operating cash",
            accountType = "Financial",
            category = "Asset",
            currencyId = "USD",
            status = "Active",
            balanceType = "BalanceForward",
            normalBalance = "Debit"
        });
        watch.Stop();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(watch.Elapsed <= CreateCeiling, $"Create took {watch.Elapsed.TotalMilliseconds:F0} ms (limit {CreateCeiling.TotalMilliseconds:F0} ms, stub data only)");
    }

    [Fact]
    public async Task NFR_GLA_002_GetAccount_CompletesWithin1Second_StubDataOnly()
    {
        var client = await WarmClientAsync();

        var watch = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/gl/accounts/1000-100-01");
        watch.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(watch.Elapsed <= OperationCeiling, $"Read took {watch.Elapsed.TotalMilliseconds:F0} ms (limit {OperationCeiling.TotalMilliseconds:F0} ms, stub data only)");
    }

    [Fact]
    public async Task NFR_GLA_003_UpdateAccount_CompletesWithin1Second_StubDataOnly()
    {
        var client = await WarmClientAsync();
        var etag = (await client.GetAsync("/api/gl/accounts/1000-100-01")).Headers.ETag!.ToString();
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/gl/accounts/1000-100-01")
        {
            Content = JsonContent.Create(new { description = "Cash (main)", category = "Asset", currencyId = "EUR", status = "Active" })
        };
        request.Headers.Add("If-Match", etag);

        var watch = Stopwatch.StartNew();
        var response = await client.SendAsync(request);
        watch.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(watch.Elapsed <= OperationCeiling, $"Update took {watch.Elapsed.TotalMilliseconds:F0} ms (limit {OperationCeiling.TotalMilliseconds:F0} ms, stub data only)");
    }

    [Fact]
    public async Task NFR_GLA_004_AddAttribute_CompletesWithin1Second_StubDataOnly()
    {
        var client = await WarmClientAsync();

        var watch = Stopwatch.StartNew();
        var response = await client.PostAsJsonAsync("/api/gl/accounts/1000-100-01/attributes", new { attributeTypeId = "project", attributeValueId = "proj-alpha" });
        watch.Stop();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(watch.Elapsed <= OperationCeiling, $"Attribute add took {watch.Elapsed.TotalMilliseconds:F0} ms (limit {OperationCeiling.TotalMilliseconds:F0} ms, stub data only)");
    }

    [Fact]
    public async Task NFR_GLA_005_DeleteDependencyCheck_On10001Transactions_CompletesWithin1Second_StubDataOnly()
    {
        var client = await WarmClientAsync();

        var watch = Stopwatch.StartNew();
        var response = await client.DeleteAsync("/api/gl/accounts/1000-200-01");
        watch.Stop();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(watch.Elapsed <= OperationCeiling, $"Dependency check took {watch.Elapsed.TotalMilliseconds:F0} ms (limit {OperationCeiling.TotalMilliseconds:F0} ms, stub data only)");
    }
}
