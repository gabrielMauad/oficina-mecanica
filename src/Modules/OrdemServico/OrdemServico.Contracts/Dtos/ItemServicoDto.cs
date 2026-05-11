namespace OrdemServico.Contracts.Dtos;

public sealed record ItemServicoDto(
    Guid ServicoId,
    int Quantidade,
    decimal PrecoUnitarioSnapshot
);
