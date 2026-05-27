namespace OrdensServico.Domain.OrdemServico;

public sealed record ItemServicoSnapshot(Guid ServicoId, int Quantidade, decimal PrecoUnitario);
