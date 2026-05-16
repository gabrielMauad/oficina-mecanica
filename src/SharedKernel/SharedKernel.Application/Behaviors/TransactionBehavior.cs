using MediatR;
using SharedKernel.Domain;

namespace SharedKernel.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPendingIntegrationEvents _pendingEvents;
    private readonly IPublisher _publisher;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        IPendingIntegrationEvents pendingEvents,
        IPublisher publisher)
    {
        _unitOfWork = unitOfWork;
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

        var domainEvents = _unitOfWork.CollectDomainEvents();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _unitOfWork.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        foreach (var publish in _pendingEvents.GetPending())
            await publish(cancellationToken);

        return response;
    }
}
