using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record OrcamentoAprovado(
    OrdemServicoId OrdemServicoId,
    OrcamentoId OrcamentoId,
    DateTime OcorridoEm
) : IDomainEvent;
