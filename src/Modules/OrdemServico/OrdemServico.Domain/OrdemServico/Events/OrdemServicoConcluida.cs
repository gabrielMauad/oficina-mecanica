using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record OrdemServicoConcluida(
    OrdemServicoId OrdemServicoId,
    DateTime EntregueEm,
    DateTime OcorridoEm
) : IDomainEvent;