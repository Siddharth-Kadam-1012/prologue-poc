using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.CreateAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GlAccounts.Api.Features.GlAccounts.ManageAttributes;

public static class ManageAttributesEndpoint
{
    public static IEndpointRouteBuilder MapManageAttributesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/gl/accounts/{id}/attributes
        endpoints.MapPost("/api/gl/accounts/{id}/attributes", async (
            string id,
            AttributeInput input,
            ManageAttributesHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.ReadUserId(httpContext.Request);
            var result = await handler.AddAttributeAsync(id, input, userId, ct);

            return result switch
            {
                ManageAttributeResult.Created created =>
                    WithETag(httpContext, created.ETag, Results.Created(created.Location, created.Attribute)),

                ManageAttributeResult.AccountNotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                ManageAttributeResult.ValidationFailed validation =>
                    Results.Json(
                        ApiValidationProblemDetails.FromErrors(validation.Errors),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json"),

                _ => Results.Json(
                    ApiProblemDetails.InternalError(),
                    statusCode: StatusCodes.Status500InternalServerError,
                    contentType: "application/problem+json")
            };
        })
        .WithName("addAccountAttribute")
        .WithTags("Attributes")
        .Produces<AccountAttribute>(StatusCodes.Status201Created, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // PUT /api/gl/accounts/{id}/attributes/{attributeId}
        endpoints.MapPut("/api/gl/accounts/{id}/attributes/{attributeId}", async (
            string id,
            string attributeId,
            AttributeInput input,
            ManageAttributesHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.ReadUserId(httpContext.Request);
            var result = await handler.UpdateAttributeAsync(id, attributeId, input, userId, ct);

            return result switch
            {
                ManageAttributeResult.Updated updated =>
                    WithETag(httpContext, updated.ETag, Results.Ok(updated.Attribute)),

                ManageAttributeResult.AccountNotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                ManageAttributeResult.AttributeNotFound notFoundAttr =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Attribute '{notFoundAttr.AttributeId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                ManageAttributeResult.ValidationFailed validation =>
                    Results.Json(
                        ApiValidationProblemDetails.FromErrors(validation.Errors),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json"),

                _ => Results.Json(
                    ApiProblemDetails.InternalError(),
                    statusCode: StatusCodes.Status500InternalServerError,
                    contentType: "application/problem+json")
            };
        })
        .WithName("updateAccountAttribute")
        .WithTags("Attributes")
        .Produces<AccountAttribute>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // DELETE /api/gl/accounts/{id}/attributes/{attributeId}
        endpoints.MapDelete("/api/gl/accounts/{id}/attributes/{attributeId}", async (
            string id,
            string attributeId,
            ManageAttributesHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.ReadUserId(httpContext.Request);
            var result = await handler.DeleteAttributeAsync(id, attributeId, userId, ct);

            return result switch
            {
                ManageAttributeResult.Deleted deleted =>
                    WithETag(httpContext, deleted.ETag, Results.NoContent()),

                ManageAttributeResult.AccountNotFound notFound =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Account '{notFound.AccountId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                ManageAttributeResult.AttributeNotFound notFoundAttr =>
                    Results.Json(
                        ApiProblemDetails.NotFound($"Attribute '{notFoundAttr.AttributeId}' was not found."),
                        statusCode: StatusCodes.Status404NotFound,
                        contentType: "application/problem+json"),

                ManageAttributeResult.ValidationFailed validation =>
                    Results.Json(
                        ApiValidationProblemDetails.FromErrors(validation.Errors),
                        statusCode: StatusCodes.Status400BadRequest,
                        contentType: "application/problem+json"),

                _ => Results.Json(
                    ApiProblemDetails.InternalError(),
                    statusCode: StatusCodes.Status500InternalServerError,
                    contentType: "application/problem+json")
            };
        })
        .WithName("removeAccountAttribute")
        .WithTags("Attributes")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return endpoints;
    }

    private static IResult WithETag(HttpContext httpContext, string etag, IResult result)
    {
        httpContext.Response.Headers.ETag = etag;
        return result;
    }
}
