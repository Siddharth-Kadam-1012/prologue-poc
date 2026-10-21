using FluentValidation;
using GlAccounts.Api.Common;
using GlAccounts.Application;
using GlAccounts.Application.Features.GlAccounts.Templates;

namespace GlAccounts.Api.Features.GlAccounts.Templates;

public static class CopyFromTemplateEndpoint
{
    public static IEndpointRouteBuilder MapCopyFromTemplateEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/gl/accounts/{accountId}/copy", async (
            string accountId,
            CopyFromTemplateRequest request,
            CopyFromTemplateHandler handler,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.Read(ctx);
            try
            {
                var result = await handler.HandleAsync(
                    new CopyFromTemplateCommand(accountId, request.NewAccountId, request.Description, userId), ct);

                return result is null
                    ? ProblemDetailsHelper.NotFoundProblem(accountId)
                    : Results.Created($"/api/gl/accounts/{result.AccountId}", result);
            }
            catch (ValidationException ex)
            {
                return ProblemDetailsHelper.ValidationProblem(ex);
            }
            catch (ConflictException ex)
            {
                return ProblemDetailsHelper.ConflictProblem(ex.Message);
            }
        });

        return app;
    }
}

public sealed record CopyFromTemplateRequest(string NewAccountId, string Description);
