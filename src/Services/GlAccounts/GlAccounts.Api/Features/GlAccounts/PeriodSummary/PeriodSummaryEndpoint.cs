using GlAccounts.Api.Common;
using GlAccounts.Application.Features.GlAccounts.PeriodSummary;

namespace GlAccounts.Api.Features.GlAccounts.PeriodSummary;

public static class PeriodSummaryEndpoint
{
    public static IEndpointRouteBuilder MapPeriodSummaryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gl/accounts/{accountId}/period-summary", async (
            string accountId,
            int? fiscalYear,
            int? fromPeriod,
            int? toPeriod,
            PeriodSummaryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(
                new PeriodSummaryQuery(accountId, fiscalYear, fromPeriod, toPeriod), ct);
            return result is null
                ? ProblemDetailsHelper.NotFoundProblem(accountId)
                : Results.Ok(result);
        });

        return app;
    }
}
