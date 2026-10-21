using GlAccounts.Api.Common;
using GlAccounts.Application.Features.GlAccounts.Transactions;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Api.Features.GlAccounts.Transactions;

public static class TransactionsEndpoint
{
    public static IEndpointRouteBuilder MapTransactionsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gl/accounts/{accountId}/transactions", async (
            string accountId,
            string? fromDate,
            string? toDate,
            string? postingStatus,
            TransactionsHandler handler,
            CancellationToken ct) =>
        {
            // Validate date parameters
            DateOnly? from = null;
            if (fromDate is not null)
            {
                if (!DateOnly.TryParse(fromDate, out var f))
                    return Results.Problem(detail: $"fromDate '{fromDate}' is not a valid date (expected YYYY-MM-DD).",
                        statusCode: 400, title: "Bad Request");
                from = f;
            }

            DateOnly? to = null;
            if (toDate is not null)
            {
                if (!DateOnly.TryParse(toDate, out var t))
                    return Results.Problem(detail: $"toDate '{toDate}' is not a valid date (expected YYYY-MM-DD).",
                        statusCode: 400, title: "Bad Request");
                to = t;
            }

            // Validate postingStatus enum
            PostingStatus? status = null;
            if (postingStatus is not null)
            {
                if (!Enum.TryParse<PostingStatus>(postingStatus, ignoreCase: true, out var ps))
                    return Results.Problem(detail: $"postingStatus '{postingStatus}' is not valid. Expected: Posted, Unposted.",
                        statusCode: 400, title: "Bad Request");
                status = ps;
            }

            var result = await handler.HandleAsync(new TransactionsQuery(accountId, from, to, status), ct);
            return result is null
                ? ProblemDetailsHelper.NotFoundProblem(accountId)
                : Results.Ok(result);
        });

        return app;
    }
}
