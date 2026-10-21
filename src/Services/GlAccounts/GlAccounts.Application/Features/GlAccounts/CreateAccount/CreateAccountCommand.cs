using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.CreateAccount;

public sealed record CreateAccountCommand(
    string AccountId,
    string Description,
    AccountType AccountType,
    string Category,
    AccountStatus Status,
    BalanceType BalanceType,
    NormalBalance NormalBalance,
    string? CurrencyId,
    string UserId);
