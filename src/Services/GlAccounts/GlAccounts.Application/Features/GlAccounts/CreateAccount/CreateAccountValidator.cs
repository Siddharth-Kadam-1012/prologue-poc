using FluentValidation;
using GlAccounts.Domain.Services;

namespace GlAccounts.Application.Features.GlAccounts.CreateAccount;

public sealed class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .MaximumLength(32)
            .Matches(@"^\d{2}-\d{3}-\d{4}$")
            .WithMessage("Account ID does not match segment format. Expected format: NN-NNN-NNNN");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(50)
            .Must((cmd, category) => ClassificationRules.IsValidCategory(cmd.AccountType, category))
            .WithMessage(cmd =>
                $"Category '{cmd.Category}' is not valid for account type '{cmd.AccountType}'. " +
                $"Allowed: {string.Join(", ", ClassificationRules.GetAllowedCategories(cmd.AccountType))}.");

        RuleFor(x => x.CurrencyId)
            .MaximumLength(10)
            .When(x => x.CurrencyId is not null);
    }
}
