using OrdensServico.Adapters.DataSources.Records;

namespace OrdensServico.Adapters.DataSources;

public interface IOrdemServicoRepository
{
    Task Adicionar(OrdemServicoRecord ordemServico, CancellationToken ct = default);
    Task<OrdemServicoRecord?> ObterPorId(Guid id, CancellationToken ct = default);
    Task Atualizar(OrdemServicoRecord ordemServico, CancellationToken ct = default);
}
