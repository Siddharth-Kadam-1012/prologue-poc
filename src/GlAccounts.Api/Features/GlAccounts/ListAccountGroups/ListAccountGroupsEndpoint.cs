using GlAccounts.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GlAccounts.Api.Features.GlAccounts.ListAccountGroups;

public static class ListAccountGroupsEndpoint
{
    public static IEndpointRouteBuilder MapListAccountGroupsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/gl/accounts/{id}/groups
        endpoints.MapGet("/api/gl/accounts/{id}/groups", async (
            string id,
            ListAccountGroupsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.GetGroupsAsync(id, ct);

            return result switch
            {
                ListAccountGroupsResult.Success success =>
                    Results.Ok(success.Response),

                ListAccountGroupsResult.NotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                ListAccountGroupsResult.InvalidId invalid =>
                    Results.Json(
                        ApiValidationProblemDetails.FromError("id", invalid.Message),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json"),

                _ => Results.Json(
                    ApiProblemDetails.InternalError(),
                    statusCode: StatusCodes.Status500InternalServerError,
                    contentType: "application/problem+json")
            };
        })
        .WithName("listAccountGroups")
        .WithTags("Groups")
        .Produces<AccountGroupListResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // GET /api/gl/accounts/{id}/groups/export
        endpoints.MapGet("/api/gl/accounts/{id}/groups/export", async (
            string id,
            ListAccountGroupsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExportGroupsAsync(id, ct);

            return result switch
            {
                ListAccountGroupsResult.ExportSuccess export =>
                    Results.File(
                        export.Content,
                        contentType: "text/csv",
                        fileDownloadName: export.FileName),

                ListAccountGroupsResult.NotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                ListAccountGroupsResult.InvalidId invalid =>
                    Results.Json(
                        ApiValidationProblemDetails.FromError("id", invalid.Message),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json"),

                _ => Results.Json(
                    ApiProblemDetails.InternalError(),
                    statusCode: StatusCodes.Status500InternalServerError,
                    contentType: "application/problem+json")
            };
        })
        .WithName("exportAccountGroups")
        .WithTags("Groups")
        .Produces(StatusCodes.Status200OK, contentType: "text/csv")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return endpoints;
    }
}
