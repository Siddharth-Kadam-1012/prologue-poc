using GlAccounts.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GlAccounts.Api.Features.GlAccounts.Templates;

public static class SearchAccountsEndpoint
{
    public static IEndpointRouteBuilder MapSearchAccountsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/gl/accounts", async (
            string? q,
            string? status,
            string? accountType,
            string? category,
            int? page,
            int? pageSize,
            SearchAccountsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(q, status, accountType, category, page, pageSize, ct);

            return result switch
            {
                SearchAccountsResult.Success success => Results.Ok(success.Page),

                SearchAccountsResult.InvalidRequest invalid => Results.Json(
                    ApiValidationProblemDetails.FromError(invalid.Field, invalid.Message),
                    statusCode: StatusCodes.Status400BadRequest,
                    contentType: "application/problem+json"),

                _ => Results.Json(
                    ApiProblemDetails.InternalError(),
                    statusCode: StatusCodes.Status500InternalServerError,
                    contentType: "application/problem+json")
            };
        })
        .WithName("searchAccounts")
        .WithTags("Accounts")
        .Produces<AccountSummaryPage>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return endpoints;
    }
}
