using GlAccounts.Application.Features.GlAccounts.SearchAccounts;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Api.Features.GlAccounts.SearchAccounts;

public static class SearchAccountsEndpoint
{
    public static IEndpointRouteBuilder MapSearchAccountsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gl/accounts", async (
            AccountStatus? status,
            SearchAccountsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new SearchAccountsQuery(status), ct);
            return Results.Ok(result);
        });

        return app;
    }
}
