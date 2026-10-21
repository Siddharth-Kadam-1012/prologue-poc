using GlAccounts.Api.Common;
using GlAccounts.Application.Features.GlAccounts.ManageAttributes;
using FluentValidation;

namespace GlAccounts.Api.Features.GlAccounts.ManageAttributes;

public static class ManageAttributesEndpoint
{
    public static IEndpointRouteBuilder MapManageAttributesEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/gl/accounts/{accountId}/attributes
        app.MapPost("/api/gl/accounts/{accountId}/attributes", async (
            string accountId,
            AddAttributeRequest request,
            AddAttributeHandler handler,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.Read(ctx);
            try
            {
                var result = await handler.HandleAsync(
                    new AddAttributeCommand(accountId, request.AttributeTypeId, request.AttributeValueId, userId), ct);
                return result is null
                    ? ProblemDetailsHelper.NotFoundProblem(accountId)
                    : Results.Created($"/api/gl/accounts/{accountId}/attributes/{result.AttributeId}", result);
            }
            catch (ValidationException ex)
            {
                return ProblemDetailsHelper.ValidationProblem(ex);
            }
        });

        // PUT /api/gl/accounts/{accountId}/attributes/{attributeId}
        app.MapPut("/api/gl/accounts/{accountId}/attributes/{attributeId}", async (
            string accountId,
            string attributeId,
            UpdateAttributeRequest request,
            UpdateAttributeHandler handler,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.Read(ctx);
            try
            {
                var result = await handler.HandleAsync(
                    new UpdateAttributeCommand(accountId, attributeId, request.AttributeValueId, userId), ct);
                return result is null
                    ? ProblemDetailsHelper.NotFoundProblem(attributeId)
                    : Results.Ok(result);
            }
            catch (ValidationException ex)
            {
                return ProblemDetailsHelper.ValidationProblem(ex);
            }
        });

        // DELETE /api/gl/accounts/{accountId}/attributes/{attributeId}
        app.MapDelete("/api/gl/accounts/{accountId}/attributes/{attributeId}", async (
            string accountId,
            string attributeId,
            RemoveAttributeHandler handler,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.Read(ctx);
            var result = await handler.HandleAsync(
                new RemoveAttributeCommand(accountId, attributeId, userId), ct);
            return result is null
                ? ProblemDetailsHelper.NotFoundProblem(attributeId)
                : Results.NoContent();
        });

        return app;
    }
}

public sealed record AddAttributeRequest(string AttributeTypeId, string AttributeValueId);
public sealed record UpdateAttributeRequest(string AttributeValueId);
