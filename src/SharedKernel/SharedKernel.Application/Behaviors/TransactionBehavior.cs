using MediatR;
using SharedKernel.Domain;

namespace SharedKernel.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : ICommand<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPendingIntegrationEvents _pendingEvents;

    public TransactionBehavior(IUnitOfWork unitOfWork, IPendingIntegrationEvents pendingEvents)
    {
        _unitOfWork = unitOfWork;
        _pendingEvents = pendingEvents;
    }

    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (response.IsFailure)
            return response;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var publish in _pendingEvents.GetPending())
            await publish(cancellationToken);

        return response;
    }
}
