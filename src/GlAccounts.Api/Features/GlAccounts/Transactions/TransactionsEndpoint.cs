using GlAccounts.Api.Common;
using GlAccounts.Api.Features.GlAccounts.StubData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GlAccounts.Api.Features.GlAccounts.Transactions;

public static class TransactionsEndpoint
{
    public static IEndpointRouteBuilder MapTransactionsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/gl/accounts/{id}/transactions
        endpoints.MapGet("/api/gl/accounts/{id}/transactions", async (
            string id,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? fiscalYear,
            int? fiscalPeriod,
            int? page,
            int? pageSize,
            TransactionsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.ListTransactionsAsync(id, fromDate, toDate, fiscalYear, fiscalPeriod, page, pageSize, ct);

            return result switch
            {
                TransactionsResult.PageSuccess success =>
                    Results.Ok(success.Page),

                TransactionsResult.NotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.Id}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                TransactionsResult.InvalidRequest invalid =>
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
        .WithName("listAccountTransactions")
        .WithTags("Transactions")
        .Produces<JournalEntryPage>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // GET /api/gl/accounts/{id}/transactions/export
        endpoints.MapGet("/api/gl/accounts/{id}/transactions/export", async (
            string id,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? fiscalYear,
            int? fiscalPeriod,
            TransactionsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExportTransactionsAsync(id, fromDate, toDate, fiscalYear, fiscalPeriod, ct);

            return result switch
            {
                TransactionsResult.ExportSuccess export =>
                    Results.File(
                        export.Content,
                        contentType: "text/csv",
                        fileDownloadName: export.FileName),

                TransactionsResult.NotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.Id}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                TransactionsResult.InvalidRequest invalid =>
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
        .WithName("exportAccountTransactions")
        .WithTags("Transactions")
        .Produces(StatusCodes.Status200OK, contentType: "text/csv")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // GET /api/gl/accounts/{id}/transactions/{entryId}
        endpoints.MapGet("/api/gl/accounts/{id}/transactions/{entryId}", async (
            string id,
            string entryId,
            TransactionsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.GetDetailAsync(id, entryId, ct);

            return result switch
            {
                TransactionsResult.DetailSuccess success =>
                    Results.Ok(success.Detail),

                TransactionsResult.NotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Transaction entry '{notFound.Id}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                TransactionsResult.InvalidRequest invalid =>
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
        .WithName("getAccountTransaction")
        .WithTags("Transactions")
        .Produces<JournalEntryDetail>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return endpoints;
    }
}
