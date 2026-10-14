namespace GlAccounts.Api.Domain;

public static class StatusRules
{
    public static (bool IsValid, string? Error) ValidateStatusTransition(
        AccountStatus currentStatus,
        AccountStatus targetStatus,
        string? justification)
    {
        if (currentStatus == targetStatus)
        {
            return (true, null);
        }

        if (currentStatus == AccountStatus.Template || targetStatus == AccountStatus.Template)
        {
            return (false, "Template status cannot be changed to or from another status.");
        }

        if (string.IsNullOrWhiteSpace(justification) || justification.Trim().Length < 1 || justification.Length > 500)
        {
            return (false, "A justification of 1 to 500 characters is required when the status changes.");
        }

        return (true, null);
    }
}
