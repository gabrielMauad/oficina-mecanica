using SharedKernel.Domain;

namespace PecasInsumos.Domain.Events;

public sealed record EstoqueAtualizado(
    PecaInsumoId Id,
    string Nome,
    int QuantidadeEmEstoque,
    DateTime OcorridoEm
) : IDomainEvent;
