namespace GlAccounts.Application;

/// <summary>
/// Thrown when a creation request conflicts with an existing resource (e.g., duplicate account ID).
/// Maps to HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
