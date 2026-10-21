using GlAccounts.Application.Ports;
using GlAccounts.Infrastructure.Stubs;
using Microsoft.Extensions.DependencyInjection;

namespace GlAccounts.Infrastructure;

/// <summary>
/// Registers all in-memory stub stores with the DI container.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddGlAccountsInfrastructure(this IServiceCollection services)
    {
        // Register concrete stubs as singletons so seed data persists for the app lifetime.
        services.AddSingleton<InMemoryAccountStore>();
        services.AddSingleton<InMemoryAccountGroupStore>();
        services.AddSingleton<InMemoryAttributeCatalogStore>();
        services.AddSingleton<InMemoryAccountAttributeStore>();
        services.AddSingleton<InMemoryPeriodBalanceStore>();
        services.AddSingleton<InMemoryTransactionStore>();
        services.AddSingleton<InMemoryAuditStore>();

        // Register via interfaces (ports) — resolve the same singleton instance.
        services.AddSingleton<IAccountStore>(sp => sp.GetRequiredService<InMemoryAccountStore>());
        services.AddSingleton<IAccountGroupStore>(sp => sp.GetRequiredService<InMemoryAccountGroupStore>());
        services.AddSingleton<IAttributeCatalogStore>(sp => sp.GetRequiredService<InMemoryAttributeCatalogStore>());
        services.AddSingleton<IAccountAttributeStore>(sp => sp.GetRequiredService<InMemoryAccountAttributeStore>());
        services.AddSingleton<IPeriodBalanceStore>(sp => sp.GetRequiredService<InMemoryPeriodBalanceStore>());
        services.AddSingleton<ITransactionStore>(sp => sp.GetRequiredService<InMemoryTransactionStore>());
        services.AddSingleton<IAuditStore>(sp => sp.GetRequiredService<InMemoryAuditStore>());

        // InMemoryAccountDependencyStore composes over ITransactionStore.
        services.AddSingleton<InMemoryAccountDependencyStore>();
        services.AddSingleton<IAccountDependencyStore>(sp => sp.GetRequiredService<InMemoryAccountDependencyStore>());

        return services;
    }
}
