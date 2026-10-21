using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.UpdateAccount;

/// <summary>
/// All fields are optional; only supplied (non-null) fields are applied.
/// Sending accountId or accountType in the body triggers a 422 (permanent field).
/// </summary>
public sealed record UpdateAccountCommand(
    string AccountId,           // from URL path — never from body
    string? Description,
    string? Category,
    AccountStatus? Status,
    BalanceType? BalanceType,
    NormalBalance? NormalBalance,
    string? CurrencyId,
    string UserId,
    // Sent-by-caller to detect attempts to change permanent fields:
    string? AttemptedAccountId,     // null = caller did not include accountId in body
    string? AttemptedAccountType);  // null = caller did not include accountType in body
