using GlAccounts.Domain.Enums;

namespace GlAccounts.Application.Features.GlAccounts.Templates;

public sealed record CopyFromTemplateResult(
    string AccountId,
    string Description,
    AccountType AccountType,
    string Category,
    AccountStatus Status,
    BalanceType BalanceType,
    NormalBalance NormalBalance,
    string? CurrencyId,
    DateOnly CreatedDate,
    string CreatedBy);
