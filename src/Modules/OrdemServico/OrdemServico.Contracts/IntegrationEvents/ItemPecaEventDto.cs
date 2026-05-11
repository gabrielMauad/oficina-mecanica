namespace OrdemServico.Contracts.IntegrationEvents;

public sealed record ItemPecaEventDto(
    Guid PecaInsumoId,
    int Quantidade
);