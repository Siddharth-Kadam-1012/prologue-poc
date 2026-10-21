using FluentValidation;

namespace GlAccounts.Api.Common;

public static class ProblemDetailsHelper
{
    public static IResult ValidationProblem(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return Results.ValidationProblem(errors, statusCode: 422);
    }

    public static IResult ConflictProblem(string detail) =>
        Results.Problem(detail: detail, statusCode: 409, title: "Conflict");

    public static IResult NotFoundProblem(string id) =>
        Results.Problem(detail: $"Resource '{id}' was not found.", statusCode: 404, title: "Not Found");
}
