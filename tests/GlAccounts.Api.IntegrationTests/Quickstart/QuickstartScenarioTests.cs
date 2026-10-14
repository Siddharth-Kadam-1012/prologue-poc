using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlAccounts.Api.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GlAccounts.Api.IntegrationTests.Quickstart;

// T014: the 18 validation scenarios of specs/001-gl-account-maintenance/quickstart.md, run end to end
// against the test host. Stub data only: nothing here proves durability, real transactions, or
// concurrency against a real database. Each scenario gets its own host, so each starts from the seed data.
public class QuickstartScenarioTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public QuickstartScenarioTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record Host(HttpClient Client, IAuditStore Audit);

    private Host NewHost()
    {
        var factory = _factory.WithWebHostBuilder(_ => { });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "demo-user");
        return new Host(client, factory.Services.GetRequiredService<IAuditStore>());
    }

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static object FinancialAccount(string accountId) => new
    {
        accountId,
        description = "Operating cash",
        accountType = "Financial",
        category = "Asset",
        currencyId = "USD",
        status = "Active",
        balanceType = "BalanceForward",
        normalBalance = "Debit"
    };

    private static HttpRequestMessage Put(string url, object body, string ifMatch)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("If-Match", ifMatch);
        return request;
    }

    private static async Task<(string ETag, JsonDocument Doc)> GetAccount(HttpClient client, string accountId)
    {
        var response = await client.GetAsync($"/api/gl/accounts/{accountId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (response.Headers.ETag!.ToString(), await Json(response));
    }

    // Update body that keeps the account's current values, with a different status.
    private static object UpdateBody(JsonElement account, string status, string? justification = null) => new
    {
        description = account.GetProperty("description").GetString(),
        category = account.GetProperty("category").GetString(),
        currencyId = account.GetProperty("currencyId").GetString(),
        status,
        statusJustification = justification
    };

    [Fact]
    public async Task Quickstart_01_CreateFinancialAccount_Returns201WithSegments_AndAuditEntry()
    {
        // AC-GLA-001, AC-GLA-049
        var host = NewHost();

        var response = await host.Client.PostAsJsonAsync("/api/gl/accounts", FinancialAccount("1000-100-02"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await Json(response);
        Assert.Equal("1000-100-02", doc.RootElement.GetProperty("accountId").GetString());
        Assert.Equal("Financial", doc.RootElement.GetProperty("accountType").GetString());
        Assert.True(doc.RootElement.TryGetProperty("segments", out _));
        var audit = Assert.Single(host.Audit.GetEntries("1000-100-02"));
        Assert.Equal(AuditEntryKind.Created, audit.Kind);
        Assert.Equal("demo-user", audit.UserId);
    }

    [Fact]
    public async Task Quickstart_02_CreateWithUnknownSegmentOrWrongSeparator_Returns400()
    {
        // AC-GLA-002, AC-GLA-044
        var host = NewHost();

        var unknownSegment = await host.Client.PostAsJsonAsync("/api/gl/accounts", FinancialAccount("1000-777-01"));
        var wrongSeparator = await host.Client.PostAsJsonAsync("/api/gl/accounts", FinancialAccount("1000/100/02"));

        foreach (var response in new[] { unknownSegment, wrongSeparator })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var doc = await Json(response);
            Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("accountId", out _));
        }

        Assert.Empty(host.Audit.GetEntries("1000-777-01"));
    }

    [Fact]
    public async Task Quickstart_03_CreateDuplicateAccountId_Returns409WithDuplicateMessage()
    {
        // AC-GLA-003
        var host = NewHost();

        var response = await host.Client.PostAsJsonAsync("/api/gl/accounts", FinancialAccount("1000-100-01"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var doc = await Json(response);
        Assert.Equal("Account ID '1000-100-01' already exists. Please use a unique ID.", doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Quickstart_04_ReadExistingAccountThenUnknown_Returns200WithETagThen404()
    {
        // AC-GLA-004, AC-GLA-005
        var host = NewHost();

        var ok = await host.Client.GetAsync("/api/gl/accounts/1000-100-01");
        var missing = await host.Client.GetAsync("/api/gl/accounts/9999-999-99");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.NotNull(ok.Headers.ETag);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Quickstart_05_UpdateDescriptionAndCurrency_ReadBackShowsNewValues_AuditHasBeforeAfter()
    {
        // AC-GLA-006, AC-GLA-007, AC-GLA-050
        var host = NewHost();
        var (etag, before) = await GetAccount(host.Client, "1000-100-01");
        using var beforeDoc = before;
        var oldDescription = before.RootElement.GetProperty("description").GetString();

        var body = new { description = "Cash (main)", category = before.RootElement.GetProperty("category").GetString(), currencyId = "EUR", status = "Active" };
        var response = await host.Client.SendAsync(Put("/api/gl/accounts/1000-100-01", body, etag));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (_, after) = await GetAccount(host.Client, "1000-100-01");
        using (after)
        {
            Assert.Equal("Cash (main)", after.RootElement.GetProperty("description").GetString());
            Assert.Equal("EUR", after.RootElement.GetProperty("currencyId").GetString());
        }

        var change = Assert.Single(host.Audit.GetEntries("1000-100-01"), e => e.Kind == AuditEntryKind.FieldChanged && e.Field == "description");
        Assert.Equal(oldDescription, change.Before);
        Assert.Equal("Cash (main)", change.After);
        Assert.Equal("demo-user", change.UserId);
    }

    [Fact]
    public async Task Quickstart_06_StaleIfMatchReturns409_PermanentFieldChangeReturns400()
    {
        // AC-GLA-056, AC-GLA-008, AC-GLA-045
        var host = NewHost();
        var (etag, account) = await GetAccount(host.Client, "1000-100-01");
        using var accountDoc = account;
        var stale = await host.Client.SendAsync(Put("/api/gl/accounts/1000-100-01", UpdateBody(account.RootElement, "Active"), "\"stale-version\""));

        var permanent = await host.Client.SendAsync(Put("/api/gl/accounts/1000-100-01", new
        {
            accountId = "1000-100-09",
            accountType = "Statistical",
            description = "x",
            category = "Asset",
            currencyId = "USD",
            status = "Active"
        }, etag));

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, permanent.StatusCode);
        using (var doc = await Json(permanent))
        {
            var errors = doc.RootElement.GetProperty("errors");
            Assert.True(errors.TryGetProperty("accountId", out _));
            Assert.True(errors.TryGetProperty("accountType", out _));
        }

        var (etagAfter, _) = await GetAccount(host.Client, "1000-100-01");
        Assert.Equal(etag, etagAfter);
    }

    [Fact]
    public async Task Quickstart_07_StatusChangeNeedsJustification_TemplateCannotChange()
    {
        // AC-GLA-022, AC-GLA-023
        var host = NewHost();
        var (etag, account) = await GetAccount(host.Client, "1000-100-01");
        using var accountDoc = account;

        var withoutJustification = await host.Client.SendAsync(Put("/api/gl/accounts/1000-100-01", UpdateBody(account.RootElement, "Hold"), etag));
        var withJustification = await host.Client.SendAsync(Put("/api/gl/accounts/1000-100-01", UpdateBody(account.RootElement, "Hold", "Under review"), etag));

        var (templateEtag, template) = await GetAccount(host.Client, "9000-100-01");
        using var templateDoc = template;
        var templateChange = await host.Client.SendAsync(Put("/api/gl/accounts/9000-100-01", UpdateBody(template.RootElement, "Active", "Activate"), templateEtag));

        Assert.Equal(HttpStatusCode.BadRequest, withoutJustification.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withJustification.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, templateChange.StatusCode);
        Assert.Contains(host.Audit.GetEntries("1000-100-01"), e => e.Kind == AuditEntryKind.StatusChanged && e.Justification == "Under review");
    }

    [Theory]
    [InlineData("1000-100-01", true)]
    [InlineData("4000-100-01", false)]
    [InlineData("5000-100-01", false)]
    [InlineData("9000-100-01", false)]
    public async Task Quickstart_08_PostingEligibility_OnlyActiveIsEligible(string accountId, bool expected)
    {
        // AC-GLA-020, AC-GLA-021
        var host = NewHost();

        var (_, account) = await GetAccount(host.Client, accountId);
        using (account)
        {
            Assert.Equal(expected, account.RootElement.GetProperty("eligibleForPosting").GetBoolean());
        }
    }

    [Fact]
    public async Task Quickstart_09_InvalidCategoryAndStatisticalWithBalanceType_Return400FieldErrors()
    {
        // AC-GLA-012, AC-GLA-013, AC-GLA-057
        var host = NewHost();

        var badCategory = await host.Client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "1000-100-02",
            description = "Bad",
            accountType = "Financial",
            category = "Statistical",
            currencyId = "USD",
            status = "Active",
            balanceType = "BalanceForward",
            normalBalance = "Debit"
        });
        var statisticalWithBalance = await host.Client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId = "7000-100-02",
            description = "Bad statistical",
            accountType = "Statistical",
            category = "Statistical",
            currencyId = "USD",
            status = "Active",
            balanceType = "BalanceForward"
        });

        Assert.Equal(HttpStatusCode.BadRequest, badCategory.StatusCode);
        using (var doc = await Json(badCategory)) Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("category", out _));
        Assert.Equal(HttpStatusCode.BadRequest, statisticalWithBalance.StatusCode);
        using (var doc = await Json(statisticalWithBalance)) Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("balanceType", out _));
    }

    [Fact]
    public async Task Quickstart_10_Delete_DependencyFreeSucceeds_BlockedAccountsListEveryBlocker()
    {
        // AC-GLA-038, AC-GLA-039, AC-GLA-040, AC-GLA-041, AC-GLA-042
        var host = NewHost();

        var deleted = await host.Client.DeleteAsync("/api/gl/accounts/1000-100-03");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/gl/accounts/1000-100-03")).StatusCode);

        async Task<List<(string Kind, int Count)>> Blockers(string accountId)
        {
            var response = await host.Client.DeleteAsync($"/api/gl/accounts/{accountId}");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using var doc = await Json(response);
            Assert.True(doc.RootElement.TryGetProperty("suggestion", out _));
            return doc.RootElement.GetProperty("blockers").EnumerateArray()
                .Select(b => (b.GetProperty("kind").GetString()!, b.GetProperty("count").GetInt32())).ToList();
        }

        Assert.Contains(("PostedTransactions", 10001), await Blockers("1000-200-01"));
        Assert.Contains(("UnpostedTransactions", 5), await Blockers("1000-300-01"));
        Assert.Contains(("Budgets", 2), await Blockers("2000-100-01"));
        Assert.Contains(("Allocations", 1), await Blockers("2000-200-01"));
        Assert.Contains(("GroupMemberships", 1), await Blockers("2000-300-01"));
        var several = await Blockers("3000-100-01");
        Assert.True(several.Count >= 2, "AC-GLA-042: all blocking dependency types are listed in one response");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/gl/accounts/3000-100-01")).StatusCode);
    }

    [Fact]
    public async Task Quickstart_11_PeriodSummary_WithBalancesAndWithout()
    {
        // AC-GLA-028, AC-GLA-029, AC-GLA-032
        var host = NewHost();

        var withBalances = await host.Client.GetAsync("/api/gl/accounts/1000-100-01/period-summary");
        var withoutBalances = await host.Client.GetAsync("/api/gl/accounts/8000-100-01/period-summary");

        Assert.Equal(HttpStatusCode.OK, withBalances.StatusCode);
        using (var doc = await Json(withBalances))
        {
            Assert.NotEmpty(doc.RootElement.GetProperty("periods").EnumerateArray().ToList());
            Assert.NotEmpty(doc.RootElement.GetProperty("years").EnumerateArray().ToList());
            Assert.NotEmpty(doc.RootElement.GetProperty("budgetVersions").EnumerateArray().ToList());
        }

        Assert.Equal(HttpStatusCode.OK, withoutBalances.StatusCode);
        using (var doc = await Json(withoutBalances))
        {
            Assert.Empty(doc.RootElement.GetProperty("periods").EnumerateArray().ToList());
        }
    }

    [Fact]
    public async Task Quickstart_12_PeriodSummary_FilterSortAndExportXlsx()
    {
        // AC-GLA-030, AC-GLA-031
        var host = NewHost();
        int year;
        using (var all = await Json(await host.Client.GetAsync("/api/gl/accounts/1000-100-01/period-summary")))
        {
            year = all.RootElement.GetProperty("periods").EnumerateArray().First().GetProperty("fiscalYear").GetInt32();
        }

        using (var filtered = await Json(await host.Client.GetAsync($"/api/gl/accounts/1000-100-01/period-summary?fiscalYear={year}&sortBy=balance&sortDirection=desc")))
        {
            var periods = filtered.RootElement.GetProperty("periods").EnumerateArray().ToList();
            Assert.NotEmpty(periods);
            Assert.All(periods, p => Assert.Equal(year, p.GetProperty("fiscalYear").GetInt32()));
            var balances = periods.Select(p => p.GetProperty("balance").GetDecimal()).ToList();
            Assert.Equal(balances.OrderByDescending(b => b).ToList(), balances);
        }

        var export = await host.Client.GetAsync($"/api/gl/accounts/1000-100-01/period-summary/export?fiscalYear={year}&sortBy=balance&sortDirection=desc");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        using var zip = new ZipArchive(new MemoryStream(await export.Content.ReadAsByteArrayAsync()));
        Assert.Contains(zip.Entries, e => e.FullName == "xl/worksheets/sheet1.xml");
    }

    [Fact]
    public async Task Quickstart_13_Transactions_DefaultPaging_DateRange_Drilldown_AndCsvExport()
    {
        // AC-GLA-033, AC-GLA-034, AC-GLA-035, AC-GLA-036
        var host = NewHost();
        string entryId, postingDate;
        using (var page = await Json(await host.Client.GetAsync("/api/gl/accounts/1000-200-01/transactions")))
        {
            Assert.Equal(50, page.RootElement.GetProperty("pageSize").GetInt32());
            Assert.Equal(50, page.RootElement.GetProperty("items").GetArrayLength());
            Assert.Equal(10001, page.RootElement.GetProperty("total").GetInt32());
            var first = page.RootElement.GetProperty("items").EnumerateArray().First();
            entryId = first.GetProperty("entryId").GetString()!;
            postingDate = first.GetProperty("postingDate").GetString()![..10];
        }

        int filteredTotal;
        using (var filtered = await Json(await host.Client.GetAsync($"/api/gl/accounts/1000-200-01/transactions?fromDate={postingDate}&toDate={postingDate}&pageSize=200")))
        {
            filteredTotal = filtered.RootElement.GetProperty("total").GetInt32();
            Assert.InRange(filteredTotal, 1, 10000);
            Assert.All(filtered.RootElement.GetProperty("items").EnumerateArray(),
                i => Assert.StartsWith(postingDate, i.GetProperty("postingDate").GetString()));
        }

        using (var detail = await Json(await host.Client.GetAsync($"/api/gl/accounts/1000-200-01/transactions/{entryId}")))
        {
            Assert.Equal(entryId, detail.RootElement.GetProperty("entryId").GetString());
            Assert.NotEmpty(detail.RootElement.GetProperty("lines").EnumerateArray().ToList());
        }

        var csv = await (await host.Client.GetAsync($"/api/gl/accounts/1000-200-01/transactions/export?fromDate={postingDate}&toDate={postingDate}")).Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal("entryId,batchId,description,amount,postingStatus,postingDate", lines[0]);
        Assert.Equal(filteredTotal, lines.Length - 1);
    }

    [Fact]
    public async Task Quickstart_14_Transactions_PageSizeAbove200_Returns400()
    {
        // AC-GLA-037
        var host = NewHost();

        var response = await host.Client.GetAsync("/api/gl/accounts/1000-200-01/transactions?pageSize=201");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Quickstart_15_Attributes_AddTwoEditOneRemoveOne_InvalidValueRejected()
    {
        // AC-GLA-014, AC-GLA-015, AC-GLA-016
        var host = NewHost();
        const string baseUrl = "/api/gl/accounts/8000-100-01/attributes";

        string firstId, secondId;
        using (var first = await Json(await host.Client.PostAsJsonAsync(baseUrl, new { attributeTypeId = "dept", attributeValueId = "finance" })))
            firstId = first.RootElement.GetProperty("attributeId").GetString()!;
        using (var second = await Json(await host.Client.PostAsJsonAsync(baseUrl, new { attributeTypeId = "region", attributeValueId = "na" })))
            secondId = second.RootElement.GetProperty("attributeId").GetString()!;
        using (var both = (await GetAccount(host.Client, "8000-100-01")).Doc)
            Assert.Equal(2, both.RootElement.GetProperty("attributes").GetArrayLength());

        var edited = await host.Client.PutAsJsonAsync($"{baseUrl}/{firstId}", new { attributeTypeId = "dept", attributeValueId = "sales" });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        var removed = await host.Client.DeleteAsync($"{baseUrl}/{secondId}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var invalid = await host.Client.PostAsJsonAsync(baseUrl, new { attributeTypeId = "dept", attributeValueId = "emea" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var final = (await GetAccount(host.Client, "8000-100-01")).Doc;
        var attribute = Assert.Single(final.RootElement.GetProperty("attributes").EnumerateArray().ToList());
        Assert.Equal("sales", attribute.GetProperty("attributeValueId").GetString());
    }

    [Fact]
    public async Task Quickstart_16_UpdateWithOneInvalidAttributeChange_AppliesNothing()
    {
        // AC-GLA-017, AC-GLA-018
        var host = NewHost();
        var (etag, before) = await GetAccount(host.Client, "1000-100-01");
        using var beforeDoc = before;
        var auditCount = host.Audit.GetEntries("1000-100-01").Count;

        var response = await host.Client.SendAsync(Put("/api/gl/accounts/1000-100-01", new
        {
            description = "Should not be saved",
            category = before.RootElement.GetProperty("category").GetString(),
            currencyId = before.RootElement.GetProperty("currencyId").GetString(),
            status = "Active",
            attributeChanges = new object[]
            {
                new { action = "Add", attributeTypeId = "project", attributeValueId = "proj-alpha" },
                new { action = "Edit", attributeId = "att-1", attributeTypeId = "dept", attributeValueId = "emea" }
            }
        }, etag));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (etagAfter, after) = await GetAccount(host.Client, "1000-100-01");
        using (after)
        {
            Assert.Equal(etag, etagAfter);
            Assert.Equal(before.RootElement.GetProperty("description").GetString(), after.RootElement.GetProperty("description").GetString());
            Assert.Equal(before.RootElement.GetProperty("attributes").GetArrayLength(), after.RootElement.GetProperty("attributes").GetArrayLength());
        }

        Assert.Equal(auditCount, host.Audit.GetEntries("1000-100-01").Count);
    }

    [Fact]
    public async Task Quickstart_17_Groups_NestedNoGroupAndCsvExport()
    {
        // AC-GLA-024, AC-GLA-025, AC-GLA-027
        var host = NewHost();

        using (var nested = await Json(await host.Client.GetAsync("/api/gl/accounts/1000-100-01/groups")))
        {
            var groups = nested.RootElement.GetProperty("items").EnumerateArray().ToList();
            Assert.NotEmpty(groups);
            Assert.Contains(groups, g => g.GetProperty("parentChain").GetArrayLength() > 0);
        }

        var none = await host.Client.GetAsync("/api/gl/accounts/8000-100-01/groups");
        Assert.Equal(HttpStatusCode.OK, none.StatusCode);
        using (var doc = await Json(none)) Assert.Empty(doc.RootElement.GetProperty("items").EnumerateArray().ToList());

        var csv = await (await host.Client.GetAsync("/api/gl/accounts/1000-100-01/groups/export")).Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal("groupId,name,parentChain", lines[0]);
        Assert.True(lines.Length > 1, "every group of the account is listed");
    }

    [Fact]
    public async Task Quickstart_18_Templates_SearchCreateFromTemplate_NonTemplateRejected()
    {
        // AC-GLA-053, AC-GLA-054, AC-GLA-055
        var host = NewHost();

        using (var search = await Json(await host.Client.GetAsync("/api/gl/accounts?status=Template")))
        {
            var items = search.RootElement.GetProperty("items").EnumerateArray().ToList();
            Assert.NotEmpty(items);
            Assert.All(items, i => Assert.Equal("Template", i.GetProperty("status").GetString()));
        }

        var created = await host.Client.PostAsJsonAsync("/api/gl/accounts", new { accountId = "6000-100-01", templateAccountId = "9000-100-01" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using (var doc = await Json(created))
        {
            Assert.Equal("6000-100-01", doc.RootElement.GetProperty("accountId").GetString());
            Assert.Equal("Active", doc.RootElement.GetProperty("status").GetString());
        }

        var nonTemplate = await host.Client.PostAsJsonAsync("/api/gl/accounts", new { accountId = "6000-100-02", templateAccountId = "1000-100-01" });
        Assert.Equal(HttpStatusCode.BadRequest, nonTemplate.StatusCode);
        using (var doc = await Json(nonTemplate))
            Assert.Contains("template", doc.RootElement.GetProperty("errors").GetProperty("templateAccountId")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }
}
