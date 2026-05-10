using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record OrdemServicoGerada(
    OrdemServicoId OrdemServicoId,
    Guid ClienteId,
    Guid VeiculoId,
    DateTime OcorridoEm
) : IDomainEvent;
