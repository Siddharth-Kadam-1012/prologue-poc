namespace GlAccounts.Domain.Entities;

/// <summary>
/// Account group — read model managed by a separate feature.
/// This feature only reads group data.
/// </summary>
public sealed class AccountGroup
{
    public required string GroupId { get; init; }
    public required string GroupName { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> AccountIds { get; init; }
}
