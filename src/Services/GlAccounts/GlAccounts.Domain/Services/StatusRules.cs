using GlAccounts.Domain.Enums;

namespace GlAccounts.Domain.Services;

/// <summary>
/// Encodes valid GL account status transitions (data model state-transition rules).
/// </summary>
public static class StatusRules
{
    /// <summary>
    /// Returns true if transitioning from <paramref name="current"/> to
    /// <paramref name="next"/> is a permitted state change.
    /// </summary>
    public static bool IsValidTransition(AccountStatus current, AccountStatus next)
    {
        if (current == next) return true;   // no-op is always valid

        return (current, next) switch
        {
            (AccountStatus.Active,   AccountStatus.Hold)     => true,
            (AccountStatus.Active,   AccountStatus.Inactive) => true,
            (AccountStatus.Active,   AccountStatus.Template) => true,
            (AccountStatus.Hold,     AccountStatus.Active)   => true,
            (AccountStatus.Hold,     AccountStatus.Inactive) => true,
            (AccountStatus.Inactive, AccountStatus.Active)   => true,
            (AccountStatus.Template, AccountStatus.Active)   => true,
            _ => false
        };
    }

    /// <summary>
    /// Returns true when an account with the given status may be referenced
    /// in new journal transactions.
    /// </summary>
    public static bool CanPostTransactions(AccountStatus status) =>
        status == AccountStatus.Active;
}
