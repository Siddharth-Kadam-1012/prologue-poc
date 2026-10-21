namespace GlAccounts.Domain.Entities;

/// <summary>
/// Period-level balance snapshot for a GL account.
/// </summary>
public sealed class PeriodBalance
{
    public required string BalanceId { get; init; }
    public required string AccountId { get; init; }
    public required int FiscalYear { get; init; }
    public required int FiscalPeriod { get; init; }
    public string? BudgetVersion { get; init; }
    public required decimal Balance { get; init; }
}
