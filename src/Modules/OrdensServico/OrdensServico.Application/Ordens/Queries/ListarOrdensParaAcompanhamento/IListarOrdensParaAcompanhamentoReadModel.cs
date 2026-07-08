using OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;

namespace OrdensServico.Application.Ordens.Queries.ListarOrdensParaAcompanhamento;

public interface IListarOrdensParaAcompanhamentoReadModel
{
    Task<List<OrdemServicoListItem>> Listar(CancellationToken ct = default);
}
