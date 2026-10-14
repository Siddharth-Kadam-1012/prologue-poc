using System.Text.Json.Serialization;

namespace GlAccounts.Api.Common;

public class ApiProblemDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "https://httpstatuses.io/500";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "An unexpected error occurred.";

    [JsonPropertyName("status")]
    public int Status { get; set; } = 500;

    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }

    [JsonPropertyName("instance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instance { get; set; }

    public static ApiProblemDetails NotFound(string message) => new()
    {
        Type = "https://httpstatuses.io/404",
        Title = "Not Found",
        Status = 404,
        Detail = message
    };

    public static ApiProblemDetails Conflict(string message) => new()
    {
        Type = "https://httpstatuses.io/409",
        Title = "Conflict",
        Status = 409,
        Detail = message
    };

    public static ApiProblemDetails InternalError() => new()
    {
        Type = "https://httpstatuses.io/500",
        Title = "An unexpected error occurred.",
        Status = 500,
        Detail = null
    };
}

public class ApiValidationProblemDetails : ApiProblemDetails
{
    public ApiValidationProblemDetails()
    {
        Type = "https://httpstatuses.io/400";
        Title = "One or more validation errors occurred.";
        Status = 400;
    }

    public ApiValidationProblemDetails(IDictionary<string, string[]> errors) : this()
    {
        Errors = errors;
    }

    [JsonPropertyName("errors")]
    public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();

    public static ApiValidationProblemDetails FromError(string field, string message)
    {
        return new ApiValidationProblemDetails(new Dictionary<string, string[]>
        {
            [field] = new[] { message }
        });
    }

    public static ApiValidationProblemDetails FromErrors(IDictionary<string, string[]> errors)
    {
        return new ApiValidationProblemDetails(errors);
    }
}

public record DeleteBlocker(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("count")] int Count);

public class DeleteBlockedProblemDetails : ApiProblemDetails
{
    public DeleteBlockedProblemDetails()
    {
        Type = "https://httpstatuses.io/409";
        Title = "Conflict";
        Status = 409;
    }

    [JsonPropertyName("blockers")]
    public List<DeleteBlocker> Blockers { get; set; } = new();

    [JsonPropertyName("suggestion")]
    public string Suggestion { get; set; } = "Set the account status to Inactive instead of deleting it.";
}
