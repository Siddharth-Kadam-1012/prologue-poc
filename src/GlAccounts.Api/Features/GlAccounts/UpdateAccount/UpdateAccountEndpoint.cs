using GlAccounts.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GlAccounts.Api.Features.GlAccounts.UpdateAccount;

public static class UpdateAccountEndpoint
{
    public static IEndpointRouteBuilder MapUpdateAccountEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/gl/accounts/{id}", async (
            string id,
            UpdateAccountRequest request,
            UpdateAccountHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var ifMatch = httpContext.Request.Headers.IfMatch.ToString();
            var userId = UserIdReader.ReadUserId(httpContext.Request);

            var result = await handler.HandleAsync(id, ifMatch, request, userId, ct);

            switch (result)
            {
                case UpdateAccountResult.Success success:
                    httpContext.Response.Headers.ETag = success.ETag;
                    return Results.Ok(success.Account);

                case UpdateAccountResult.NotFound notFound:
                    return Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json");

                case UpdateAccountResult.ValidationFailed validation:
                    return Results.Json(
                        ApiValidationProblemDetails.FromErrors(validation.Errors),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json");

                case UpdateAccountResult.StaleVersionConflict conflict:
                    return Results.Json(
                        ApiProblemDetails.Conflict(conflict.Message),
                        statusCode: StatusCodes.Status409Conflict,
                        contentType: "application/problem+json");

                default:
                    return Results.Json(
                        ApiProblemDetails.InternalError(),
                        statusCode: StatusCodes.Status500InternalServerError,
                        contentType: "application/problem+json");
            }
        })
        .WithName("updateAccount")
        .WithTags("Accounts")
        .Produces<Domain.Account>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return endpoints;
    }
}
