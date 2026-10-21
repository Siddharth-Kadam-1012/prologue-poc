using GlAccounts.Api.Common;
using GlAccounts.Application.Features.GlAccounts.GetAccount;

namespace GlAccounts.Api.Features.GlAccounts.GetAccount;

public static class GetAccountEndpoint
{
    public static IEndpointRouteBuilder MapGetAccountEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gl/accounts/{accountId}", async (
            string accountId,
            GetAccountHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetAccountQuery(accountId), ct);
            return result is null
                ? ProblemDetailsHelper.NotFoundProblem(accountId)
                : Results.Ok(result);
        });

        return app;
    }
}
