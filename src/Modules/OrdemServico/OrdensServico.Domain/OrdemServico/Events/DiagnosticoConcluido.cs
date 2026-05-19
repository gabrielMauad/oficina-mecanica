using SharedKernel.Domain;

namespace OrdensServico.Domain.OrdemServico.Events;

public sealed record DiagnosticoConcluido(
    OrdemServicoId OrdemServicoId,
    OrcamentoId OrcamentoId,
    string DescricaoDiagnostico,
    IReadOnlyList<ItemServicoSnapshot> Servicos,
    IReadOnlyList<ItemPecaSnapshot> Pecas,
    decimal ValorTotal,
    DateTime OcorridoEm
) : IDomainEvent;
