using GlAccounts.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GlAccounts.Api.Features.GlAccounts.CreateAccount;

public static class CreateAccountEndpoint
{
    public static IEndpointRouteBuilder MapCreateAccountEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/gl/accounts", async (
            CreateAccountRequest request,
            CreateAccountHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.ReadUserId(httpContext.Request);
            var result = await handler.HandleAsync(request, userId, ct);

            switch (result)
            {
                case CreateAccountResult.Success success:
                    httpContext.Response.Headers.ETag = success.ETag;
                    return Results.Created($"/api/gl/accounts/{success.Account.AccountId}", success.Account);

                case CreateAccountResult.ValidationFailed validation:
                    return Results.Json(
                        ApiValidationProblemDetails.FromErrors(validation.Errors),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json");

                case CreateAccountResult.Duplicate duplicate:
                    return Results.Json(
                        ApiProblemDetails.Conflict($"Account ID '{duplicate.AccountId}' already exists. Please use a unique ID."),
                        statusCode: StatusCodes.Status409Conflict,
                        contentType: "application/problem+json");

                case CreateAccountResult.TemplateNotFound templateNotFound:
                    return Results.Json(
                        ApiProblemDetails.NotFound($"Template account '{templateNotFound.TemplateAccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json");

                default:
                    return Results.Json(
                        ApiProblemDetails.InternalError(),
                        statusCode: StatusCodes.Status500InternalServerError,
                        contentType: "application/problem+json");
            }
        })
        .WithName("createAccount")
        .WithTags("Accounts")
        .Produces<Domain.Account>(StatusCodes.Status201Created, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return endpoints;
    }
}
