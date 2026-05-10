using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record OrdemServicoEmExecucao(
    OrdemServicoId OrdemServicoId,
    DateTime OcorridoEm
) : IDomainEvent;