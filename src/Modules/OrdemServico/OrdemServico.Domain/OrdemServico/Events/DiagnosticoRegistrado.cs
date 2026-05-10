using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record DiagnosticoRegistrado(
    OrdemServicoId OrdemServicoId,
    string DescricaoDiagnostico,
    DateTime OcorridoEm
) : IDomainEvent;