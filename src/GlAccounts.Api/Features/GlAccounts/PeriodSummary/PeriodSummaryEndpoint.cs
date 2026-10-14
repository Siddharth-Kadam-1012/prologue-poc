using GlAccounts.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GlAccounts.Api.Features.GlAccounts.PeriodSummary;

public static class PeriodSummaryEndpoint
{
    public static IEndpointRouteBuilder MapPeriodSummaryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/gl/accounts/{id}/period-summary
        endpoints.MapGet("/api/gl/accounts/{id}/period-summary", async (
            string id,
            int? fiscalYear,
            string? sortBy,
            string? sortDirection,
            PeriodSummaryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.GetSummaryAsync(id, fiscalYear, sortBy, sortDirection, ct);

            return result switch
            {
                PeriodSummaryResult.Success success =>
                    Results.Ok(success.Summary),

                PeriodSummaryResult.NotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                PeriodSummaryResult.InvalidRequest invalid =>
                    Results.Json(
                        ApiValidationProblemDetails.FromError(invalid.Field, invalid.Message),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json"),

                _ => Results.Json(
                    ApiProblemDetails.InternalError(),
                    statusCode: StatusCodes.Status500InternalServerError,
                    contentType: "application/problem+json")
            };
        })
        .WithName("getPeriodSummary")
        .WithTags("Period summary")
        .Produces<StubData.PeriodSummary>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // GET /api/gl/accounts/{id}/period-summary/export
        endpoints.MapGet("/api/gl/accounts/{id}/period-summary/export", async (
            string id,
            int? fiscalYear,
            string? sortBy,
            string? sortDirection,
            PeriodSummaryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExportSummaryAsync(id, fiscalYear, sortBy, sortDirection, ct);

            return result switch
            {
                PeriodSummaryResult.ExportSuccess export =>
                    Results.File(
                        export.Content,
                        contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileDownloadName: export.FileName),

                PeriodSummaryResult.NotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                PeriodSummaryResult.InvalidRequest invalid =>
                    Results.Json(
                        ApiValidationProblemDetails.FromError(invalid.Field, invalid.Message),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json"),

                _ => Results.Json(
                    ApiProblemDetails.InternalError(),
                    statusCode: StatusCodes.Status500InternalServerError,
                    contentType: "application/problem+json")
            };
        })
        .WithName("exportPeriodSummary")
        .WithTags("Period summary")
        .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return endpoints;
    }
}
