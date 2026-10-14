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

public class CreateAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Reset store before test run if needed
            });
        });
    }

    [Fact]
    public async Task AC_GLA_001_NFR_GLA_001_PostAccount_Returns201WithLocationAndETag()
    {
        // REQ-GLA-001, AC-GLA-001, NFR-GLA-001
        var client = _factory.CreateClient();

        var request = new CreateAccountRequest
        {
            AccountId = "1000-100-02",
            Description = "Operating cash 2",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit
        };

        var start = DateTime.UtcNow;
        var response = await client.PostAsJsonAsync("/api/gl/accounts", request);
        var elapsed = DateTime.UtcNow - start;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(elapsed.TotalSeconds < 2.0, "NFR-GLA-001: create operation <= 2 s");

        Assert.Equal("/api/gl/accounts/1000-100-02", response.Headers.Location?.ToString());
        Assert.NotNull(response.Headers.ETag);

        var body = await response.Content.ReadFromJsonAsync<Account>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(body);
        Assert.Equal("1000-100-02", body.AccountId);
        Assert.Equal(3, body.Segments.Count);
        Assert.Equal("Operating cash 2", body.Description);
        Assert.True(body.EligibleForPosting);
    }

    [Fact]
    public async Task AC_GLA_002_PostAccount_InvalidSegment_Returns400ValidationProblem()
    {
        // REQ-GLA-002, AC-GLA-002, AC-GLA-043
        var client = _factory.CreateClient();

        var request = new CreateAccountRequest
        {
            AccountId = "1000-777-01",
            Description = "Invalid dept account",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit
        };

        var response = await client.PostAsJsonAsync("/api/gl/accounts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ApiValidationProblemDetails>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.True(problem.Errors.ContainsKey("accountId"));
        Assert.Contains(problem.Errors["accountId"], e => e.Contains("Department '777' is not an allowed value"));
    }

    [Fact]
    public async Task AC_GLA_003_PostAccount_Duplicate_Returns409Conflict()
    {
        // REQ-GLA-003, AC-GLA-003, D-02
        var client = _factory.CreateClient();

        var request = new CreateAccountRequest
        {
            AccountId = "1000-100-01", // Seeded account
            Description = "Duplicate cash account",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit
        };

        var response = await client.PostAsJsonAsync("/api/gl/accounts", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Contains("already exists", problem.Detail);
    }
}
