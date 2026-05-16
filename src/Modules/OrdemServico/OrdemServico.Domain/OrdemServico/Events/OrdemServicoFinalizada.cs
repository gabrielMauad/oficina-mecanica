using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record OrdemServicoFinalizada(
    OrdemServicoId OrdemServicoId,
    Guid ClienteId,
    DateTime OcorridoEm
) : IDomainEvent;
