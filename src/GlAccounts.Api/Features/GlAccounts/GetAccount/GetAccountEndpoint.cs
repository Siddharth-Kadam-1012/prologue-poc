using GlAccounts.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GlAccounts.Api.Features.GlAccounts.GetAccount;

public static class GetAccountEndpoint
{
    public static IEndpointRouteBuilder MapGetAccountEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/gl/accounts/{id}", async (
            string id,
            GetAccountHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);

            switch (result)
            {
                case GetAccountResult.Success success:
                    httpContext.Response.Headers.ETag = success.ETag;
                    return Results.Ok(success.Account);

                case GetAccountResult.NotFound notFound:
                    return Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json");

                case GetAccountResult.InvalidId invalidId:
                    return Results.Json(
                        ApiValidationProblemDetails.FromError("id", invalidId.Message),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json");

                default:
                    return Results.Json(
                        ApiProblemDetails.InternalError(),
                        statusCode: StatusCodes.Status500InternalServerError,
                        contentType: "application/problem+json");
            }
        })
        .WithName("getAccount")
        .WithTags("Accounts")
        .Produces<Domain.Account>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return endpoints;
    }
}
