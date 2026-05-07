using SharedKernel.Domain;

namespace PecasInsumos.Domain.PecasInsumos.Events;

public sealed record EstoqueEsgotado(
    PecaInsumoId PecaInsumoId,
    string Nome,
    DateTime OcorridoEm
) : IDomainEvent;
