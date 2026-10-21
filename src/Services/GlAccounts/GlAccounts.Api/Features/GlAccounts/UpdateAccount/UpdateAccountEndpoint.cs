using FluentValidation;
using GlAccounts.Api.Common;
using GlAccounts.Application;
using GlAccounts.Application.Features.GlAccounts.UpdateAccount;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Api.Features.GlAccounts.UpdateAccount;

public static class UpdateAccountEndpoint
{
    public static IEndpointRouteBuilder MapUpdateAccountEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/gl/accounts/{accountId}", async (
            string accountId,
            UpdateAccountRequest request,
            UpdateAccountHandler handler,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.Read(ctx);
            try
            {
                var result = await handler.HandleAsync(new UpdateAccountCommand(
                    accountId,
                    request.Description,
                    request.Category,
                    request.Status,
                    request.BalanceType,
                    request.NormalBalance,
                    request.CurrencyId,
                    userId,
                    request.AccountId,        // AttemptedAccountId — null if caller didn't send it
                    request.AccountType),     // AttemptedAccountType — null if caller didn't send it
                    ct);

                return result is null
                    ? ProblemDetailsHelper.NotFoundProblem(accountId)
                    : Results.Ok(result);
            }
            catch (ValidationException ex)
            {
                return ProblemDetailsHelper.ValidationProblem(ex);
            }
        });

        return app;
    }
}

public sealed record UpdateAccountRequest(
    string? Description,
    string? Category,
    AccountStatus? Status,
    BalanceType? BalanceType,
    NormalBalance? NormalBalance,
    string? CurrencyId,
    // Permanent-field change detection: if caller includes these, validator rejects them
    string? AccountId,
    string? AccountType);
