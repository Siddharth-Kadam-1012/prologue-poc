using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace GlAccounts.Api.IntegrationTests.Contract;

// T012: contract conformance against specs/001-gl-account-maintenance/contracts/openapi.yaml.
// Stub data only. The app does not generate an OpenAPI document, so these tests read the committed
// contract, check that every operation is routed, then call each operation and compare status codes,
// content types, headers and required schema properties. Error responses 500 are not triggerable
// against stub data and are not exercised.
public class ContractConformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ProblemJson = "application/problem+json";
    private const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly string[] AccountProps =
        { "accountId", "segments", "description", "accountType", "category", "currencyId", "status", "createdDate", "eligibleForPosting", "attributes" };

    private readonly WebApplicationFactory<Program> _factory;

    public ContractConformanceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // Fresh seeded stub store per test: each call builds its own host.
    private WebApplicationFactory<Program> NewHost() => _factory.WithWebHostBuilder(_ => { });

    private static string ContractPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "specs", "001-gl-account-maintenance", "contracts", "openapi.yaml");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("specs/001-gl-account-maintenance/contracts/openapi.yaml not found above " + AppContext.BaseDirectory);
    }

    private record ContractOperation(string Method, string Path, string OperationId);

    private static List<ContractOperation> ReadContractOperations()
    {
        var ops = new List<ContractOperation>();
        string? path = null;
        string? method = null;
        var inPaths = false;

        foreach (var line in File.ReadLines(ContractPath()))
        {
            if (line.StartsWith("paths:")) { inPaths = true; continue; }
            if (line.StartsWith("components:")) break;
            if (!inPaths) continue;

            var p = Regex.Match(line, @"^  (/api\S*):\s*$");
            if (p.Success) { path = p.Groups[1].Value; method = null; continue; }

            var m = Regex.Match(line, @"^    (get|post|put|delete|patch):\s*$");
            if (m.Success) { method = m.Groups[1].Value.ToUpperInvariant(); continue; }

            var o = Regex.Match(line, @"^      operationId:\s*(\S+)\s*$");
            if (o.Success && path is not null && method is not null)
            {
                ops.Add(new ContractOperation(method, path, o.Groups[1].Value));
            }
        }

        return ops;
    }

    private static void AssertProblem(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static void AssertHas(JsonElement element, params string[] props)
    {
        foreach (var prop in props)
        {
            Assert.True(element.TryGetProperty(prop, out _), $"Missing required property '{prop}' in {element.GetRawText()}");
        }
    }

    private static async Task AssertValidation(HttpResponseMessage response)
    {
        AssertProblem(response, HttpStatusCode.BadRequest);
        using var doc = await ReadJson(response);
        AssertHas(doc.RootElement, "status", "errors");
        Assert.Equal(JsonValueKind.Object, doc.RootElement.GetProperty("errors").ValueKind);
    }

    private static async Task AssertNotFound(HttpResponseMessage response)
    {
        AssertProblem(response, HttpStatusCode.NotFound);
        using var doc = await ReadJson(response);
        Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
    }

    private static HttpRequestMessage Put(string url, object body, string? ifMatch)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(body) };
        if (ifMatch is not null) request.Headers.Add("If-Match", ifMatch);
        return request;
    }

    private static async Task<string> ReadETag(HttpClient client, string accountId)
    {
        var get = await client.GetAsync($"/api/gl/accounts/{accountId}");
        return get.Headers.ETag!.ToString();
    }

    private static readonly string LongId = new('9', 33);

    [Fact]
    public void API_GLA_ALL_EveryContractOperationIsRoutedWithSameMethodPathAndOperationId()
    {
        // API-GLA-001..016: all 15 operationIds of the contract (API-GLA-016 is createAccount with templateAccountId)
        var contract = ReadContractOperations();
        Assert.Equal(15, contract.Count);

        using var host = NewHost();
        var routed = host.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => new
            {
                Path = e.RoutePattern.RawText,
                Name = e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName,
                Methods = e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? Array.Empty<string>()
            })
            .ToList();

        foreach (var op in contract)
        {
            Assert.Contains(routed, r => r.Name == op.OperationId && r.Path == op.Path && r.Methods.Contains(op.Method));
        }

        var contractIds = contract.Select(o => o.OperationId).ToHashSet();
        var extra = routed.Where(r => r.Name is not null && !contractIds.Contains(r.Name)).Select(r => r.Name).ToList();
        Assert.Empty(extra);
    }

    [Fact]
    public async Task API_GLA_015_searchAccounts_ConformsToContract()
    {
        var client = NewHost().CreateClient();

        var response = await client.GetAsync("/api/gl/accounts?status=Template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var doc = await ReadJson(response);
        AssertHas(doc.RootElement, "items", "total", "page", "pageSize");
        var item = doc.RootElement.GetProperty("items").EnumerateArray().First();
        AssertHas(item, "accountId", "description", "accountType", "category", "currencyId", "status");

        await AssertValidation(await client.GetAsync("/api/gl/accounts?pageSize=201"));
    }

    [Fact]
    public async Task API_GLA_001_API_GLA_016_createAccount_ConformsToContract()
    {
        var client = NewHost().CreateClient();

        var created = await client.PostAsJsonAsync("/api/gl/accounts", new
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
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("application/json", created.Content.Headers.ContentType?.MediaType);
        Assert.Equal("/api/gl/accounts/1000-100-02", created.Headers.Location?.ToString());
        Assert.NotNull(created.Headers.ETag);
        using (var doc = await ReadJson(created)) AssertHas(doc.RootElement, AccountProps);

        var fromTemplate = await client.PostAsJsonAsync("/api/gl/accounts", new { accountId = "6000-100-01", templateAccountId = "9000-100-01" });
        Assert.Equal(HttpStatusCode.Created, fromTemplate.StatusCode);
        Assert.NotNull(fromTemplate.Headers.ETag);
        using (var doc = await ReadJson(fromTemplate)) AssertHas(doc.RootElement, AccountProps);

        await AssertValidation(await client.PostAsJsonAsync("/api/gl/accounts", new { accountId = "1000-777-01" }));
        await AssertNotFound(await client.PostAsJsonAsync("/api/gl/accounts", new { accountId = "6000-100-03", templateAccountId = "9999-999-99" }));

        var duplicate = await client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "1000-100-01",
            description = "Dup",
            accountType = "Financial",
            category = "Asset",
            currencyId = "USD",
            status = "Active",
            balanceType = "BalanceForward",
            normalBalance = "Debit"
        });
        AssertProblem(duplicate, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task API_GLA_002_getAccount_ConformsToContract()
    {
        var client = NewHost().CreateClient();

        var ok = await client.GetAsync("/api/gl/accounts/1000-100-01");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("application/json", ok.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(ok.Headers.ETag);
        using (var doc = await ReadJson(ok)) AssertHas(doc.RootElement, AccountProps);

        await AssertNotFound(await client.GetAsync("/api/gl/accounts/9999-999-99"));
        await AssertValidation(await client.GetAsync($"/api/gl/accounts/{LongId}"));
    }

    [Fact]
    public async Task API_GLA_003_updateAccount_ConformsToContract()
    {
        var client = NewHost().CreateClient();
        var etag = await ReadETag(client, "1000-100-01");
        var body = new { description = "Cash (main)", category = "Asset", currencyId = "EUR", status = "Active" };

        var ok = await client.SendAsync(Put("/api/gl/accounts/1000-100-01", body, etag));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("application/json", ok.Content.Headers.ContentType?.MediaType);
        Assert.NotEqual(etag, ok.Headers.ETag?.ToString());
        using (var doc = await ReadJson(ok)) AssertHas(doc.RootElement, AccountProps);

        await AssertValidation(await client.SendAsync(Put("/api/gl/accounts/1000-100-01", body, null)));
        await AssertNotFound(await client.SendAsync(Put("/api/gl/accounts/9999-999-99", body, "\"any\"")));
        AssertProblem(await client.SendAsync(Put("/api/gl/accounts/1000-100-01", body, etag)), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task API_GLA_004_deleteAccount_ConformsToContract()
    {
        var client = NewHost().CreateClient();

        var deleted = await client.DeleteAsync("/api/gl/accounts/1000-100-03");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsByteArrayAsync());

        var blocked = await client.DeleteAsync("/api/gl/accounts/1000-200-01");
        AssertProblem(blocked, HttpStatusCode.Conflict);
        using (var doc = await ReadJson(blocked))
        {
            AssertHas(doc.RootElement, "blockers", "suggestion");
            var blocker = doc.RootElement.GetProperty("blockers").EnumerateArray().First();
            AssertHas(blocker, "kind", "count");
        }

        await AssertNotFound(await client.DeleteAsync("/api/gl/accounts/9999-999-99"));
        await AssertValidation(await client.DeleteAsync($"/api/gl/accounts/{LongId}"));
    }

    [Fact]
    public async Task API_GLA_005_addAccountAttribute_ConformsToContract()
    {
        var client = NewHost().CreateClient();
        var before = await ReadETag(client, "1000-100-01");

        var created = await client.PostAsJsonAsync("/api/gl/accounts/1000-100-01/attributes", new { attributeTypeId = "project", attributeValueId = "proj-alpha" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("application/json", created.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(created.Headers.ETag);
        Assert.NotEqual(before, created.Headers.ETag!.ToString());
        using (var doc = await ReadJson(created)) AssertHas(doc.RootElement, "attributeId", "attributeTypeId", "attributeValueId");

        await AssertValidation(await client.PostAsJsonAsync("/api/gl/accounts/1000-100-01/attributes", new { attributeTypeId = "dept", attributeValueId = "emea" }));
        await AssertNotFound(await client.PostAsJsonAsync("/api/gl/accounts/9999-999-99/attributes", new { attributeTypeId = "dept", attributeValueId = "sales" }));
    }

    [Fact]
    public async Task API_GLA_006_updateAccountAttribute_ConformsToContract()
    {
        var client = NewHost().CreateClient();
        var before = await ReadETag(client, "1000-100-01");

        var ok = await client.PutAsJsonAsync("/api/gl/accounts/1000-100-01/attributes/att-1", new { attributeTypeId = "dept", attributeValueId = "sales" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("application/json", ok.Content.Headers.ContentType?.MediaType);
        Assert.NotEqual(before, ok.Headers.ETag?.ToString());
        using (var doc = await ReadJson(ok)) AssertHas(doc.RootElement, "attributeId", "attributeTypeId", "attributeValueId");

        await AssertValidation(await client.PutAsJsonAsync("/api/gl/accounts/1000-100-01/attributes/att-1", new { attributeTypeId = "dept", attributeValueId = "emea" }));
        await AssertNotFound(await client.PutAsJsonAsync("/api/gl/accounts/1000-100-01/attributes/att-999", new { attributeTypeId = "dept", attributeValueId = "sales" }));
    }

    [Fact]
    public async Task API_GLA_007_removeAccountAttribute_ConformsToContract()
    {
        var client = NewHost().CreateClient();
        var before = await ReadETag(client, "1000-100-01");

        var removed = await client.DeleteAsync("/api/gl/accounts/1000-100-01/attributes/att-2");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.NotNull(removed.Headers.ETag);
        Assert.NotEqual(before, removed.Headers.ETag!.ToString());

        await AssertNotFound(await client.DeleteAsync("/api/gl/accounts/1000-100-01/attributes/att-999"));
        await AssertNotFound(await client.DeleteAsync("/api/gl/accounts/9999-999-99/attributes/att-1"));
    }

    [Fact]
    public async Task API_GLA_008_listAccountGroups_ConformsToContract()
    {
        var client = NewHost().CreateClient();

        var ok = await client.GetAsync("/api/gl/accounts/1000-100-01/groups");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("application/json", ok.Content.Headers.ContentType?.MediaType);
        using (var doc = await ReadJson(ok))
        {
            AssertHas(doc.RootElement, "items");
            var group = doc.RootElement.GetProperty("items").EnumerateArray().First();
            AssertHas(group, "groupId", "name", "parentChain");
            foreach (var parent in group.GetProperty("parentChain").EnumerateArray()) AssertHas(parent, "groupId", "name");
        }

        await AssertNotFound(await client.GetAsync("/api/gl/accounts/9999-999-99/groups"));
        await AssertValidation(await client.GetAsync($"/api/gl/accounts/{LongId}/groups"));
    }

    [Fact]
    public async Task API_GLA_009_exportAccountGroups_ConformsToContract()
    {
        var client = NewHost().CreateClient();

        var ok = await client.GetAsync("/api/gl/accounts/1000-100-01/groups/export");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("text/csv", ok.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(ok.Content.Headers.ContentDisposition?.FileName);
        Assert.StartsWith("groupId,name,parentChain", await ok.Content.ReadAsStringAsync());

        await AssertNotFound(await client.GetAsync("/api/gl/accounts/9999-999-99/groups/export"));
        await AssertValidation(await client.GetAsync($"/api/gl/accounts/{LongId}/groups/export"));
    }

    [Fact]
    public async Task API_GLA_010_getPeriodSummary_ConformsToContract()
    {
        var client = NewHost().CreateClient();

        var ok = await client.GetAsync("/api/gl/accounts/1000-100-01/period-summary");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("application/json", ok.Content.Headers.ContentType?.MediaType);
        using (var doc = await ReadJson(ok))
        {
            AssertHas(doc.RootElement, "accountId", "periods", "years", "budgetVersions");
            AssertHas(doc.RootElement.GetProperty("periods").EnumerateArray().First(), "fiscalYear", "fiscalPeriod", "balance");
            AssertHas(doc.RootElement.GetProperty("years").EnumerateArray().First(), "fiscalYear", "balance");
            AssertHas(doc.RootElement.GetProperty("budgetVersions").EnumerateArray().First(), "budgetVersion", "fiscalYear", "amount");
        }

        await AssertValidation(await client.GetAsync("/api/gl/accounts/1000-100-01/period-summary?sortBy=bogus"));
        await AssertNotFound(await client.GetAsync("/api/gl/accounts/9999-999-99/period-summary"));
    }

    [Fact]
    public async Task API_GLA_011_exportPeriodSummary_ConformsToContract()
    {
        var client = NewHost().CreateClient();

        var ok = await client.GetAsync("/api/gl/accounts/1000-100-01/period-summary/export");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(XlsxType, ok.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(ok.Content.Headers.ContentDisposition?.FileName);
        Assert.True((await ok.Content.ReadAsByteArrayAsync()).Length > 0);

        await AssertValidation(await client.GetAsync("/api/gl/accounts/1000-100-01/period-summary/export?sortDirection=sideways"));
        await AssertNotFound(await client.GetAsync("/api/gl/accounts/9999-999-99/period-summary/export"));
    }

    [Fact]
    public async Task API_GLA_012_API_GLA_013_API_GLA_014_transactions_ConformToContract()
    {
        var client = NewHost().CreateClient();

        // API-GLA-012 listAccountTransactions
        var list = await client.GetAsync("/api/gl/accounts/1000-200-01/transactions?pageSize=2");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal("application/json", list.Content.Headers.ContentType?.MediaType);
        string entryId;
        using (var doc = await ReadJson(list))
        {
            AssertHas(doc.RootElement, "items", "total", "page", "pageSize");
            var first = doc.RootElement.GetProperty("items").EnumerateArray().First();
            AssertHas(first, "entryId", "batchId", "description", "amount", "postingStatus", "postingDate");
            entryId = first.GetProperty("entryId").GetString()!;
        }

        await AssertValidation(await client.GetAsync("/api/gl/accounts/1000-200-01/transactions?pageSize=201"));
        await AssertNotFound(await client.GetAsync("/api/gl/accounts/9999-999-99/transactions"));

        // API-GLA-013 getAccountTransaction
        var detail = await client.GetAsync($"/api/gl/accounts/1000-200-01/transactions/{entryId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal("application/json", detail.Content.Headers.ContentType?.MediaType);
        using (var doc = await ReadJson(detail))
        {
            AssertHas(doc.RootElement, "entryId", "batchId", "description", "amount", "postingStatus", "postingDate", "fiscalYear", "fiscalPeriod", "lines");
            AssertHas(doc.RootElement.GetProperty("lines").EnumerateArray().First(), "lineNumber", "accountId", "debit", "credit");
        }

        await AssertNotFound(await client.GetAsync("/api/gl/accounts/1000-200-01/transactions/no-such-entry"));

        // API-GLA-014 exportAccountTransactions
        var export = await client.GetAsync("/api/gl/accounts/1000-200-01/transactions/export");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("text/csv", export.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(export.Content.Headers.ContentDisposition?.FileName);
        Assert.StartsWith("entryId,batchId,description,amount,postingStatus,postingDate", await export.Content.ReadAsStringAsync());

        await AssertValidation(await client.GetAsync("/api/gl/accounts/1000-200-01/transactions/export?fiscalPeriod=1"));
        await AssertNotFound(await client.GetAsync("/api/gl/accounts/9999-999-99/transactions/export"));
    }
}
