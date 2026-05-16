namespace OrdemServico.Domain.OrdemServico;

public sealed record ItemPecaInput(Guid PecaInsumoId, int Quantidade, decimal PrecoUnitario);
