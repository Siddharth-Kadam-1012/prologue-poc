using GlAccounts.Api.Common;
using GlAccounts.Application.Features.GlAccounts.ListAccountGroups;

namespace GlAccounts.Api.Features.GlAccounts.ListAccountGroups;

public static class ListAccountGroupsEndpoint
{
    public static IEndpointRouteBuilder MapListAccountGroupsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gl/accounts/{accountId}/groups", async (
            string accountId,
            ListAccountGroupsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new ListAccountGroupsQuery(accountId), ct);
            return result is null
                ? ProblemDetailsHelper.NotFoundProblem(accountId)
                : Results.Ok(result);
        });

        return app;
    }
}
