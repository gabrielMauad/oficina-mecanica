using SharedKernel.Domain;

namespace PecasInsumos.Domain.Events;

public sealed record EstoqueEsgotado(
    PecaInsumoId PecaInsumoId,
    string Nome,
    DateTime OcorridoEm
) : IDomainEvent;
