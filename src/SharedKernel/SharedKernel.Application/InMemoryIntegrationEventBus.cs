using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Domain;

namespace SharedKernel.Application;

public sealed class InMemoryIntegrationEventBus : IIntegrationEventBus
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryIntegrationEventBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Publish<T>(T evento, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        var handlers = _serviceProvider.GetServices<IIntegrationEventHandler<T>>();

        foreach (var handler in handlers)
            await handler.Handle(evento, cancellationToken);
    }
}
