using Microsoft.AspNetCore.Http;

namespace GlAccounts.Api.Common;

public static class UserIdReader
{
    public const string HeaderName = "X-User-Id";
    public const string DefaultUnknown = "unknown";
    public const int MaxLength = 64;

    public static string ReadUserId(HttpRequest request)
    {
        if (request.Headers.TryGetValue(HeaderName, out var values))
        {
            var value = values.ToString().Trim();
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Length > MaxLength)
                {
                    value = value[..MaxLength];
                }
                return value;
            }
        }

        return DefaultUnknown;
    }

    public static string ReadUserId(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(HeaderName, out var values))
        {
            var value = values.ToString().Trim();
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Length > MaxLength)
                {
                    value = value[..MaxLength];
                }
                return value;
            }
        }

        return DefaultUnknown;
    }
}
