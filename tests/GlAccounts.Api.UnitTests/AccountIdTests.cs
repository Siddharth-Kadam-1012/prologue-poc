using GlAccounts.Api.Domain;

namespace GlAccounts.Api.UnitTests;

public class AccountIdTests
{
    [Theory]
    [InlineData("1000-100-01", "1000", "100", "01")]
    [InlineData("2000-200-02", "2000", "200", "02")]
    [InlineData("6000-100-01", "6000", "100", "01")]
    [InlineData("9000-500-05", "9000", "500", "05")]
    public void Validate_ValidAccountId_ReturnsParsedSegments(
        string accountId,
        string expectedMain,
        string expectedDept,
        string expectedSub)
    {
        // Act
        var result = AccountIdParser.Validate(accountId);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Segments);
        Assert.Equal(3, result.Segments.Count);
        Assert.Equal("Main", result.Segments[0].Name);
        Assert.Equal(expectedMain, result.Segments[0].Value);
        Assert.Equal("Department", result.Segments[1].Name);
        Assert.Equal(expectedDept, result.Segments[1].Value);
        Assert.Equal("Sub", result.Segments[2].Name);
        Assert.Equal(expectedSub, result.Segments[2].Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_NullOrEmpty_ReturnsError(string? accountId)
    {
        var result = AccountIdParser.Validate(accountId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("required"));
    }

    [Fact]
    public void Validate_Exceeds32Chars_ReturnsError()
    {
        var longId = "1000-100-01-" + new string('9', 30);
        var result = AccountIdParser.Validate(longId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("32 characters"));
    }

    [Theory]
    [InlineData("1000-100")]
    [InlineData("1000")]
    [InlineData("1000-100-01-02")]
    public void Validate_WrongSegmentCount_ReturnsError(string accountId)
    {
        var result = AccountIdParser.Validate(accountId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("segment format"));
    }

    [Theory]
    [InlineData("100-100-01")] // Main 3 digits
    [InlineData("10000-100-01")] // Main 5 digits
    [InlineData("1000-10-01")] // Dept 2 digits
    [InlineData("1000-1000-01")] // Dept 4 digits
    [InlineData("1000-100-1")] // Sub 1 digit
    [InlineData("1000-100-001")] // Sub 3 digits
    public void Validate_WrongSegmentLengths_ReturnsError(string accountId)
    {
        var result = AccountIdParser.Validate(accountId);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("1000/100/01")]
    [InlineData("1000.100.01")]
    [InlineData("1000_100_01")]
    public void Validate_WrongSeparator_ReturnsError(string accountId)
    {
        var result = AccountIdParser.Validate(accountId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("segment format"));
    }

    [Fact]
    public void Validate_UnknownDepartmentSegment_ReturnsActionableError()
    {
        // REQ-GLA-002, AC-GLA-002: Unknown value like 777
        var result = AccountIdParser.Validate("1000-777-01");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Department '777' is not an allowed value"));
    }

    [Fact]
    public void Validate_UnknownMainSegment_ReturnsActionableError()
    {
        var result = AccountIdParser.Validate("1111-100-01");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Main '1111' is not an allowed value"));
    }

    [Fact]
    public void Validate_UnknownSubSegment_ReturnsActionableError()
    {
        var result = AccountIdParser.Validate("1000-100-99");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Sub '99' is not an allowed value"));
    }
}
