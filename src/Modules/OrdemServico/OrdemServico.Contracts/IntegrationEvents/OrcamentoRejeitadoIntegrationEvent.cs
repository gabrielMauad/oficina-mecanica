using SharedKernel.Domain;

namespace OrdemServico.Contracts.IntegrationEvents;

public sealed record OrcamentoRejeitadoIntegrationEvent(
    Guid EventId,
    DateTime OcorridoEm,
    Guid OrdemServicoId,
    Guid OrcamentoId,
    IReadOnlyList<ItemPecaEventDto> Pecas
) : IIntegrationEvent;
