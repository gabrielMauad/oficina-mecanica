using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record DiagnosticoIniciado(
    OrdemServicoId OrdemServicoId,
    DateTime OcorridoEm
) : IDomainEvent;
