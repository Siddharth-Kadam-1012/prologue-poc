using FluentValidation;
using GlAccounts.Domain.Services;

namespace GlAccounts.Application.Features.GlAccounts.UpdateAccount;

public sealed class UpdateAccountValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountValidator()
    {
        // Reject attempts to change permanent fields (BR-GLA-003)
        RuleFor(x => x.AttemptedAccountId)
            .Null()
            .WithMessage("AccountId cannot be changed after creation.")
            .WithName("accountId");

        RuleFor(x => x.AttemptedAccountType)
            .Null()
            .WithMessage("AccountType cannot be changed after creation.")
            .WithName("accountType");

        RuleFor(x => x.Description)
            .MaximumLength(200)
            .When(x => x.Description is not null);

        RuleFor(x => x.Category)
            .MaximumLength(50)
            .When(x => x.Category is not null);

        RuleFor(x => x.CurrencyId)
            .MaximumLength(10)
            .When(x => x.CurrencyId is not null);
    }
}
