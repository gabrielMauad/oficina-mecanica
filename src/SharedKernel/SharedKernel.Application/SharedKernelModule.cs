using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Application.Behaviors;

namespace SharedKernel.Application;

public static class SharedKernelModule
{
    public static IServiceCollection AddSharedKernelServices(this IServiceCollection services)
    {
        services.AddScoped<IPendingIntegrationEvents, PendingIntegrationEvents>();
        services.AddSingleton<IIntegrationEventBus, InMemoryIntegrationEventBus>();

        services.AddMediatR(cfg =>
        {
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services;
    }
}

