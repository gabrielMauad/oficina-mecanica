using SharedKernel.Domain;

namespace PecasInsumos.Domain.PecasInsumos.Events;

public sealed record EstoqueAtualizado(
    PecaInsumoId Id,
    string Nome,
    int QuantidadeEmEstoque,
    DateTime OcorridoEm
) : IDomainEvent;
