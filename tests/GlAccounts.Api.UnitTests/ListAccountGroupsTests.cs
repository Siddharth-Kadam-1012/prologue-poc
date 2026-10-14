using System.Text;
using GlAccounts.Api.Features.GlAccounts.ListAccountGroups;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.UnitTests;

public class ListAccountGroupsTests
{
    private readonly InMemoryAccountStore _accountStore;
    private readonly InMemoryAccountGroupStore _groupStore;
    private readonly ListAccountGroupsHandler _handler;

    public ListAccountGroupsTests()
    {
        _accountStore = new InMemoryAccountStore();
        _groupStore = new InMemoryAccountGroupStore();
        _handler = new ListAccountGroupsHandler(_accountStore, _groupStore);
    }

    [Fact]
    public async Task AC_GLA_024_AC_GLA_026_GetGroups_AccountWithHierarchy_ReturnsGroupAndParentChain()
    {
        // REQ-GLA-015, AC-GLA-024, AC-GLA-026
        var result = await _handler.GetGroupsAsync("1000-100-01");

        var success = Assert.IsType<ListAccountGroupsResult.Success>(result);
        var items = success.Response.Items;

        Assert.NotEmpty(items);
        var group = items.First(g => g.GroupId == "grp-revenue-na");
        Assert.Equal("North America Revenue", group.Name);
        Assert.NotEmpty(group.ParentChain);
        Assert.Equal("grp-revenue", group.ParentChain[0].GroupId);
        Assert.Equal("Revenue Accounts", group.ParentChain[0].Name);
        Assert.Equal("grp-all", group.ParentChain[1].GroupId);
    }

    [Fact]
    public async Task AC_GLA_025_GetGroups_AccountInNoGroups_ReturnsEmptyItems()
    {
        // REQ-GLA-015, AC-GLA-025
        var result = await _handler.GetGroupsAsync("8000-100-01");

        var success = Assert.IsType<ListAccountGroupsResult.Success>(result);
        Assert.Empty(success.Response.Items);
    }

    [Fact]
    public async Task AC_GLA_027_ExportGroups_GeneratesCsvWithHeadersAndHierarchy()
    {
        // REQ-GLA-016, AC-GLA-027
        var result = await _handler.ExportGroupsAsync("1000-100-01");

        var export = Assert.IsType<ListAccountGroupsResult.ExportSuccess>(result);
        Assert.Equal("account-groups-1000-100-01.csv", export.FileName);

        var csv = Encoding.UTF8.GetString(export.Content);
        Assert.StartsWith("groupId,name,parentChain", csv);
        Assert.Contains("grp-revenue-na", csv);
        Assert.Contains("Revenue Accounts > All Accounts", csv);
    }

    [Fact]
    public async Task AC_GLA_024_GetGroups_RootGroup_HasEmptyParentChain()
    {
        // REQ-GLA-015, AC-GLA-024: root group has an empty parentChain (stub data only)
        var result = await _handler.GetGroupsAsync("3000-100-01");

        var success = Assert.IsType<ListAccountGroupsResult.Success>(result);
        var group = Assert.Single(success.Response.Items);
        Assert.Equal("grp-all", group.GroupId);
        Assert.Empty(group.ParentChain);
    }

    [Fact]
    public async Task AC_GLA_025_AC_GLA_027_ExportGroups_AccountInNoGroups_ReturnsOnlyHeader()
    {
        // REQ-GLA-016, AC-GLA-025, AC-GLA-027
        var result = await _handler.ExportGroupsAsync("8000-100-01");

        var export = Assert.IsType<ListAccountGroupsResult.ExportSuccess>(result);
        var lines = Encoding.UTF8.GetString(export.Content).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(new[] { "groupId,name,parentChain" }, lines);
    }

    [Fact]
    public async Task AC_GLA_027_ExportGroups_RootGroupRow_HasEmptyParentChainColumn()
    {
        // REQ-GLA-016, AC-GLA-027
        var result = await _handler.ExportGroupsAsync("3000-100-01");

        var export = Assert.IsType<ListAccountGroupsResult.ExportSuccess>(result);
        var lines = Encoding.UTF8.GetString(export.Content).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("grp-all,All Accounts,", lines[1]);
    }

    [Fact]
    public async Task AC_GLA_027_ExportGroups_NonExistentAccount_ReturnsNotFound()
    {
        var result = await _handler.ExportGroupsAsync("9999-999-99");

        Assert.IsType<ListAccountGroupsResult.NotFound>(result);
    }

    [Fact]
    public async Task GetGroups_NonExistentAccount_ReturnsNotFound()
    {
        var result = await _handler.GetGroupsAsync("9999-999-99");

        var notFound = Assert.IsType<ListAccountGroupsResult.NotFound>(result);
        Assert.Equal("9999-999-99", notFound.AccountId);
    }
}
