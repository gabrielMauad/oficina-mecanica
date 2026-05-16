namespace OrdemServico.Domain.OrdemServico;

public sealed record ItemServicoSnapshot(Guid ServicoId, int Quantidade, decimal PrecoUnitario);
