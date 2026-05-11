using OrdemServico.Contracts.Dtos;

namespace OrdemServico.Contracts.Queries;

public interface IListarOrdensPorClienteQuery
{
    Task<IReadOnlyList<OrdemServicoResumoDto>> Listar(Guid clienteId, CancellationToken ct);
}
