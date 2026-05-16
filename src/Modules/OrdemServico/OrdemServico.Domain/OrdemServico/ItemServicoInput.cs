namespace OrdemServico.Domain.OrdemServico;

public sealed record ItemServicoInput(Guid ServicoId, int Quantidade, decimal PrecoUnitario);
