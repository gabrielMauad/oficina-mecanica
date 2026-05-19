namespace OrdensServico.Contracts.Dtos;

public sealed record ItemPecaDto(
    Guid PecaInsumoId,
    int Quantidade,
    decimal PrecoUnitarioSnapshot
);
