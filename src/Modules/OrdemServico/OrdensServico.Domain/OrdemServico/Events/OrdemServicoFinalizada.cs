using SharedKernel.Domain;

namespace OrdensServico.Domain.OrdemServico.Events;

public sealed record OrdemServicoFinalizada(
    OrdemServicoId OrdemServicoId,
    Guid ClienteId,
    DateTime OcorridoEm
) : IDomainEvent;
