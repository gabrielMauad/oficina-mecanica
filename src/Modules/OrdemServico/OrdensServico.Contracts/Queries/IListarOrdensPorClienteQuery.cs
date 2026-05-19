using OrdensServico.Contracts.Dtos;

namespace OrdensServico.Contracts.Queries;

public interface IListarOrdensPorClienteQuery
{
    Task<IReadOnlyList<OrdemServicoResumoDto>> Listar(Guid clienteId, CancellationToken ct);
}
