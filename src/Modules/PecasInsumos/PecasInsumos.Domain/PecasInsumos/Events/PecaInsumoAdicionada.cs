using SharedKernel.Domain;

namespace PecasInsumos.Domain.PecasInsumos.Events;

public sealed record PecaInsumoAdicionada(
    PecaInsumoId Id,
    string Nome,
    int QuantidadeEmEstoque,
    DateTime OcorridoEm
) : IDomainEvent;
