using GlAccounts.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GlAccounts.Api.Features.GlAccounts.DeleteAccount;

public static class DeleteAccountEndpoint
{
    public static IEndpointRouteBuilder MapDeleteAccountEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/gl/accounts/{id}", async (
            string id,
            DeleteAccountHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);

            switch (result)
            {
                case DeleteAccountResult.Success:
                    return Results.NoContent();

                case DeleteAccountResult.NotFound notFound:
                    return Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json");

                case DeleteAccountResult.InvalidId invalidId:
                    return Results.Json(
                        ApiValidationProblemDetails.FromError("id", invalidId.Message),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json");

                case DeleteAccountResult.Blocked blocked:
                    var problem = new DeleteBlockedProblemDetails
                    {
                        Detail = blocked.Detail,
                        Blockers = blocked.Blockers,
                        Suggestion = blocked.Suggestion
                    };
                    return Results.Json(
                        problem,
                        statusCode: StatusCodes.Status409Conflict,
                        contentType: "application/problem+json");

                default:
                    return Results.Json(
                        ApiProblemDetails.InternalError(),
                        statusCode: StatusCodes.Status500InternalServerError,
                        contentType: "application/problem+json");
            }
        })
        .WithName("deleteAccount")
        .WithTags("Accounts")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<DeleteBlockedProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return endpoints;
    }
}
