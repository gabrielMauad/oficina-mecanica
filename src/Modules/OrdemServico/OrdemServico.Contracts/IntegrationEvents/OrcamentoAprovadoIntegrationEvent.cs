using SharedKernel.Domain;

namespace OrdemServico.Contracts.IntegrationEvents;

public sealed record OrcamentoAprovadoIntegrationEvent(
    Guid EventId,
    DateTime OcorridoEm,
    Guid OrdemServicoId,
    Guid OrcamentoId,
    IReadOnlyList<ItemPecaEventDto> Itens
  ) : IIntegrationEvent;
