using OrdensServico.Contracts.Dtos;

namespace OrdensServico.Contracts.Queries;

public interface IOrdemServicoResumoQuery
{
    Task<OrdemServicoResumoDto> ObterPorId(Guid id, CancellationToken ct);
}
