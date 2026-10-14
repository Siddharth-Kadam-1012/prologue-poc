using GlAccounts.Api.Common;
using GlAccounts.Api.Features.GlAccounts.CreateAccount;
using GlAccounts.Api.Features.GlAccounts.DeleteAccount;
using GlAccounts.Api.Features.GlAccounts.GetAccount;
using GlAccounts.Api.Features.GlAccounts.ListAccountGroups;
using GlAccounts.Api.Features.GlAccounts.ManageAttributes;
using GlAccounts.Api.Features.GlAccounts.PeriodSummary;
using GlAccounts.Api.Features.GlAccounts.StubData;
using GlAccounts.Api.Features.GlAccounts.Templates;
using GlAccounts.Api.Features.GlAccounts.Transactions;
using GlAccounts.Api.Features.GlAccounts.UpdateAccount;
using Microsoft.Extensions.DependencyInjection;

namespace GlAccounts.Api.Features.GlAccounts;

public static class GlAccountsFeatureExtensions
{
    public static IServiceCollection AddGlAccountsFeature(this IServiceCollection services)
    {
        services.AddSingleton<IAuditStore, InMemoryAuditStore>();
        services.AddSingleton<IAccountStore, InMemoryAccountStore>();
        services.AddSingleton<IAttributeCatalogStore, InMemoryAttributeCatalogStore>();
        services.AddSingleton<IAccountGroupStore, InMemoryAccountGroupStore>();
        services.AddSingleton<IAccountDependencyStore, InMemoryAccountDependencyStore>();
        services.AddSingleton<ITransactionStore, InMemoryTransactionStore>();
        services.AddSingleton<IPeriodBalanceStore, InMemoryPeriodBalanceStore>();

        services.AddScoped<CreateAccountHandler>();
        services.AddScoped<GetAccountHandler>();
        services.AddScoped<UpdateAccountHandler>();
        services.AddScoped<DeleteAccountHandler>();
        services.AddScoped<PeriodSummaryHandler>();
        services.AddScoped<TransactionsHandler>();
        services.AddScoped<ManageAttributesHandler>();
        services.AddScoped<ListAccountGroupsHandler>();
        services.AddScoped<SearchAccountsHandler>();

        return services;
    }
}
