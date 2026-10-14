using System.Text.RegularExpressions;

namespace GlAccounts.Api.Domain;

public record SegmentValidationResult(
    bool IsValid,
    IReadOnlyList<Segment>? Segments,
    IReadOnlyList<string> Errors);

public static class AccountIdParser
{
    public const string ExpectedFormat = "9999-999-99";
    public const int MaxLength = 32;

    public static readonly IReadOnlyList<string> AllowedMainSegments = new[]
    {
        "1000", "2000", "3000", "4000", "5000", "6000", "7000", "8000", "9000"
    };

    public static readonly IReadOnlyList<string> AllowedDepartmentSegments = new[]
    {
        "100", "200", "300", "400", "500"
    };

    public static readonly IReadOnlyList<string> AllowedSubSegments = new[]
    {
        "01", "02", "03", "04", "05"
    };

    public static SegmentValidationResult Validate(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return new SegmentValidationResult(false, null, new[] { "Account ID is required." });
        }

        if (accountId.Length > MaxLength)
        {
            return new SegmentValidationResult(false, null, new[] { $"Account ID must not exceed {MaxLength} characters." });
        }

        var parts = accountId.Split('-');
        if (parts.Length != 3)
        {
            return new SegmentValidationResult(
                false,
                null,
                new[] { $"Account ID does not match segment format. Expected format: {ExpectedFormat}." });
        }

        var main = parts[0];
        var dept = parts[1];
        var sub = parts[2];

        var errors = new List<string>();

        if (main.Length != 4 || !Regex.IsMatch(main, @"^\d{4}$"))
        {
            errors.Add($"Account ID does not match segment format. Expected format: {ExpectedFormat} (segment Main '{main}' is not 4 digits).");
        }
        else if (!AllowedMainSegments.Contains(main))
        {
            errors.Add($"Account ID does not match segment format. Expected format: {ExpectedFormat} (segment Main '{main}' is not an allowed value).");
        }

        if (dept.Length != 3 || !Regex.IsMatch(dept, @"^\d{3}$"))
        {
            errors.Add($"Account ID does not match segment format. Expected format: {ExpectedFormat} (segment Department '{dept}' is not 3 digits).");
        }
        else if (!AllowedDepartmentSegments.Contains(dept))
        {
            errors.Add($"Account ID does not match segment format. Expected format: {ExpectedFormat} (segment Department '{dept}' is not an allowed value).");
        }

        if (sub.Length != 2 || !Regex.IsMatch(sub, @"^\d{2}$"))
        {
            errors.Add($"Account ID does not match segment format. Expected format: {ExpectedFormat} (segment Sub '{sub}' is not 2 digits).");
        }
        else if (!AllowedSubSegments.Contains(sub))
        {
            errors.Add($"Account ID does not match segment format. Expected format: {ExpectedFormat} (segment Sub '{sub}' is not an allowed value).");
        }

        if (errors.Count > 0)
        {
            return new SegmentValidationResult(false, null, errors);
        }

        var segments = new List<Segment>
        {
            new("Main", main),
            new("Department", dept),
            new("Sub", sub)
        };

        return new SegmentValidationResult(true, segments, Array.Empty<string>());
    }
}
