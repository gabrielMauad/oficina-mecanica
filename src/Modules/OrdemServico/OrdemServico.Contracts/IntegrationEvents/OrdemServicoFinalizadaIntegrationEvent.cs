using SharedKernel.Domain;

namespace OrdemServico.Contracts.IntegrationEvents;

public sealed record OrdemServicoFinalizadaIntegrationEvent(
    Guid EventId,
    DateTime OcorridoEm,
    Guid OrdemServicoId,
    Guid ClienteId
 ) : IIntegrationEvent;
