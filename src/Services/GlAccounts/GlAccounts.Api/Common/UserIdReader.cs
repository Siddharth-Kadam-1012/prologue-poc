namespace GlAccounts.Api.Common;

public static class UserIdReader
{
    public static string Read(HttpContext ctx) =>
        ctx.Request.Headers.TryGetValue("X-User-Id", out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : "anonymous";
}
