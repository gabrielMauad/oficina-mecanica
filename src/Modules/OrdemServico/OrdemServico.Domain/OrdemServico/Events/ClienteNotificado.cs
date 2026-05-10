using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record ClienteNotificado(
    OrdemServicoId OrdemServicoId,
    DateTime NotificadoEm,
    DateTime OcorridoEm
) : IDomainEvent;
