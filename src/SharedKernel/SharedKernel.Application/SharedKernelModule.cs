using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Application;

public static class SharedKernelModule
{
    public static IServiceCollection AddSharedKernelServices(this IServiceCollection services)
    {
        services.AddScoped<IPendingIntegrationEvents, PendingIntegrationEvents>();
        services.AddScoped<IIntegrationEventBus, InMemoryIntegrationEventBus>();
        services.AddScoped<IDomainEventCollector, DomainEventCollector>();

        return services;
    }
}

