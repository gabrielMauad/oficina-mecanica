using MediatR;
using PecasInsumos.Contracts.IntegrationEvents;
using PecasInsumos.Domain;
using SharedKernel.Application;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.DecrementarEstoque;

public sealed class DecrementarEstoqueHandler : IRequestHandler<DecrementarEstoqueCommand, Result<DecrementarEstoqueResponse>>
{
    private readonly IPecaInsumoRepository _repository;
    private readonly IIntegrationEventBus _bus;
    private readonly IPendingIntegrationEvents _pendingEvents;

    public DecrementarEstoqueHandler(
        IPecaInsumoRepository repository,
        IIntegrationEventBus bus,
        IPendingIntegrationEvents pendingEvents
    )
    {
        _repository = repository;
        _bus = bus;
        _pendingEvents = pendingEvents;
    }

    public async Task<Result<DecrementarEstoqueResponse>> Handle(DecrementarEstoqueCommand command, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(command.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _repository.ObterPorId(pecaInsumoId, cancellationToken);

        if (pecaInsumo == null)
            return PecaInsumoErrors.NaoEncontrado;
        if (!pecaInsumo.Ativo)
            return PecaInsumoErrors.JaDesativado;

        Result<PecaInsumo> pecaInsumoResult = pecaInsumo.Decrementar(command.Quantidade);
        if (pecaInsumoResult.IsFailure)
            return pecaInsumoResult.Error;

        pecaInsumo = pecaInsumoResult.Value;

        await _repository.Atualizar(pecaInsumo, cancellationToken);

        _pendingEvents.Enqueue(ct =>
            _bus.Publish(
                new EstoqueDecrementadoIntegrationEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    pecaInsumo.Id.Value,
                    command.Quantidade,
                    pecaInsumo.QuantidadeEmEstoque
                )
            , ct)
        );

        return DecrementarEstoqueResponse.FromPecaInsumo(pecaInsumo);
    }
}

