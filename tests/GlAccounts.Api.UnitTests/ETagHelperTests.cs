using GlAccounts.Api.Common;

namespace GlAccounts.Api.UnitTests;

public class ETagHelperTests
{
    [Fact]
    public void FormatETag_EnclosesVersionInDoubleQuotes()
    {
        var formatted = ETagHelper.FormatETag("abc123token");
        Assert.Equal("\"abc123token\"", formatted);
    }

    [Fact]
    public void FormatETag_AlreadyQuoted_DoesNotDoubleQuote()
    {
        var formatted = ETagHelper.FormatETag("\"abc123token\"");
        Assert.Equal("\"abc123token\"", formatted);
    }

    [Theory]
    [InlineData("\"version-1\"", "version-1", true)]
    [InlineData("version-1", "version-1", true)]
    [InlineData("\"version-1\"", "\"version-1\"", true)]
    [InlineData("\"version-1\"", "version-2", false)]
    [InlineData(null, "version-1", false)]
    [InlineData("", "version-1", false)]
    public void Matches_HandlesQuotesAndMatchingCorrectly(string? ifMatch, string currentVersion, bool expected)
    {
        var result = ETagHelper.Matches(ifMatch, currentVersion);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NewVersion_ReturnsNonEmptyUniqueString()
    {
        var v1 = ETagHelper.NewVersion();
        var v2 = ETagHelper.NewVersion();

        Assert.False(string.IsNullOrWhiteSpace(v1));
        Assert.False(string.IsNullOrWhiteSpace(v2));
        Assert.NotEqual(v1, v2);
    }
}
