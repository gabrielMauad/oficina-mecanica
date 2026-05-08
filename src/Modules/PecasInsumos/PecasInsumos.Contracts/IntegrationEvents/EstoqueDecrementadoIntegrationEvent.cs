using SharedKernel.Domain;

namespace PecasInsumos.Contracts.IntegrationEvents;

public record EstoqueDecrementadoIntegrationEvent(
    Guid EventId,
    DateTime OcorridoEm,
    Guid PecaInsumoId,
    int QuantidadeDecrementada,
    int QuantidadeRestante
) : IIntegrationEvent;
