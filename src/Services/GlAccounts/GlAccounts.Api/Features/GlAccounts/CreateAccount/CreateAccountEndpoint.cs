using FluentValidation;
using GlAccounts.Api.Common;
using GlAccounts.Application;
using GlAccounts.Application.Features.GlAccounts.CreateAccount;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Api.Features.GlAccounts.CreateAccount;

public static class CreateAccountEndpoint
{
    public static IEndpointRouteBuilder MapCreateAccountEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/gl/accounts", async (
            CreateAccountRequest request,
            CreateAccountHandler handler,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = UserIdReader.Read(ctx);
            try
            {
                var result = await handler.HandleAsync(new CreateAccountCommand(
                    request.AccountId,
                    request.Description,
                    request.AccountType,
                    request.Category,
                    request.Status,
                    request.BalanceType,
                    request.NormalBalance,
                    request.CurrencyId,
                    userId), ct);

                return Results.Created($"/api/gl/accounts/{result.AccountId}", result);
            }
            catch (ValidationException ex)
            {
                return ProblemDetailsHelper.ValidationProblem(ex);
            }
            catch (ConflictException ex)
            {
                return ProblemDetailsHelper.ConflictProblem(ex.Message);
            }
        });

        return app;
    }
}

public sealed record CreateAccountRequest(
    string AccountId,
    string Description,
    AccountType AccountType,
    string Category,
    AccountStatus Status,
    BalanceType BalanceType,
    NormalBalance NormalBalance,
    string? CurrencyId);
