using SharedKernel.Domain;

namespace OrdensServico.Domain.OrdemServico.Events;

public sealed record OrcamentoRejeitado(
    OrdemServicoId OrdemServicoId,
    OrcamentoId OrcamentoId,
    IReadOnlyList<ItemPecaSnapshot> Pecas,
    DateTime OcorridoEm
) : IDomainEvent;
