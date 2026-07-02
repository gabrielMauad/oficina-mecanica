using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Adapters.DataSources;

public interface IOrdemServicoRepository
{
    Task Adicionar(OrdemServico ordemServico, CancellationToken ct = default);
    Task<OrdemServico?> ObterPorId(OrdemServicoId id, CancellationToken ct = default);
    Task Atualizar(OrdemServico ordemServico, CancellationToken ct = default);
}
