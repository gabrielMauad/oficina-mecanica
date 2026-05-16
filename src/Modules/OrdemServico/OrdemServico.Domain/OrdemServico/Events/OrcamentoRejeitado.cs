using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico.Events;

public sealed record OrcamentoRejeitado(
    OrdemServicoId OrdemServicoId,
    OrcamentoId OrcamentoId,
    IReadOnlyList<ItemPecaSnapshot> Pecas,
    DateTime OcorridoEm
) : IDomainEvent;
