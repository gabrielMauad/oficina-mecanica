namespace OrdensServico.Domain.OrdemServico;

public interface IOrdemServicoRepository
{
    Task Adicionar(OrdemServico cliente, CancellationToken ct = default);
    Task<OrdemServico?> ObterPorId(OrdemServicoId id, CancellationToken ct = default);
    Task Atualizar(OrdemServico cliente, CancellationToken ct = default);
}
