using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record OrcamentoGerado(
    OrdemServicoId OrdemServicoId,
    OrcamentoId OrcamentoId,
    decimal ValorTotal,
    DateTime OcorridoEm
) : IDomainEvent;
