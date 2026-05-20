using OrdensServico.Contracts.Dtos;

namespace OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;

public interface IListarOrdensPorClienteQuery
{
    Task<List<OrdemServicoResumoDto>> Listar(Guid clienteId, CancellationToken ct = default);
}
