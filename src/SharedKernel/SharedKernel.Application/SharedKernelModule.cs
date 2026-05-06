using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Application;

public static class SharedKernelModule
{
    public static IServiceCollection AddSharedKernelServices(this IServiceCollection services)
    {

        services.AddScoped<IPendingIntegrationEvents, PendingIntegrationEvents>();
        services.AddSingleton<IIntegrationEventBus, InMemoryIntegrationEventBus>();

        return services;
    }
}

