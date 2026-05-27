using MediatR;
using SharedKernel.Domain;

namespace SharedKernel.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand
{
    private readonly IEnumerable<IUnitOfWork> _unitOfWorks;
    private readonly IPendingIntegrationEvents _pendingEvents;
    private readonly IPublisher _publisher;

    public TransactionBehavior(
        IEnumerable<IUnitOfWork> unitOfWorks,
        IPendingIntegrationEvents pendingEvents,
        IPublisher publisher)
    {
        _unitOfWorks = unitOfWorks;
        _pendingEvents = pendingEvents;
        _publisher = publisher;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (response is IResult { IsFailure: true })
            return response;

        // Coleta domain events de todos os contextos antes de persistir
        // (contextos sem entidades rastreadas retornam lista vazia — no-op)
        var domainEvents = _unitOfWorks
            .SelectMany(uow => uow.CollectDomainEvents())
            .ToList();

        // Persiste cada contexto — no-op para os que não têm alterações rastreadas
        foreach (var uow in _unitOfWorks)
            await uow.SaveChangesAsync(cancellationToken);

        foreach (var uow in _unitOfWorks)
            uow.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        foreach (var publish in _pendingEvents.GetPending())
            await publish(cancellationToken);

        return response;
    }
}
