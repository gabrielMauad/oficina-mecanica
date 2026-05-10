using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record OrcamentoEnviado(
    OrdemServicoId OrdemServicoId,
    OrcamentoId OrcamentoId,
    DateTime DataEnvio,
    DateTime OcorridoEm
) : IDomainEvent;

