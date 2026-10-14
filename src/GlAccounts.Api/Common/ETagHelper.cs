namespace GlAccounts.Api.Common;

public static class ETagHelper
{
    public static string FormatETag(string version)
    {
        var clean = version.Trim().Trim('"');
        return $"\"{clean}\"";
    }

    public static string ExtractVersion(string etag)
    {
        return etag.Trim().Trim('"');
    }

    public static bool Matches(string? ifMatchHeader, string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(ifMatchHeader))
        {
            return false;
        }

        var incoming = ExtractVersion(ifMatchHeader);
        var current = ExtractVersion(currentVersion);

        return string.Equals(incoming, current, StringComparison.Ordinal);
    }

    public static string NewVersion()
    {
        return Guid.NewGuid().ToString("N");
    }
}
