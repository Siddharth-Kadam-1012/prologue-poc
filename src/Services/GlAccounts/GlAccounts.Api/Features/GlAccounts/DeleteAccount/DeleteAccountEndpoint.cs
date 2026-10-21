using GlAccounts.Api.Common;
using GlAccounts.Application;
using GlAccounts.Application.Features.GlAccounts.DeleteAccount;

namespace GlAccounts.Api.Features.GlAccounts.DeleteAccount;

public static class DeleteAccountEndpoint
{
    public static IEndpointRouteBuilder MapDeleteAccountEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/gl/accounts/{accountId}", async (
            string accountId,
            DeleteAccountHandler handler,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.Read(ctx);
            try
            {
                var result = await handler.HandleAsync(new DeleteAccountCommand(accountId, userId), ct);
                return result is null
                    ? ProblemDetailsHelper.NotFoundProblem(accountId)
                    : Results.NoContent();
            }
            catch (ConflictException ex)
            {
                return ProblemDetailsHelper.ConflictProblem(ex.Message);
            }
        });

        return app;
    }
}
