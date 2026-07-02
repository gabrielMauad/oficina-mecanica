namespace OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;

public interface IListarOrdensPorClienteReadModel
{
    Task<List<OrdemServicoListItem>> Listar(Guid clienteId, CancellationToken ct = default);
}
