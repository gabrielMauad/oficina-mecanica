using MediatR;
using SharedKernel.Domain;

namespace SharedKernel.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPendingIntegrationEvents _pendingEvents;

    public TransactionBehavior(IUnitOfWork unitOfWork, IPendingIntegrationEvents pendingEvents)
    {
        _unitOfWork = unitOfWork;
        _pendingEvents = pendingEvents;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (response is IResult { IsFailure: true })
            return response;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var publish in _pendingEvents.GetPending())
            await publish(cancellationToken);

        return response;
    }
}
