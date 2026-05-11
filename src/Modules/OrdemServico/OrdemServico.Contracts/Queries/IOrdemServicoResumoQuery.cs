using OrdemServico.Contracts.Dtos;

namespace OrdemServico.Contracts.Queries;

public interface IOrdemServicoResumoQuery
{
    Task<OrdemServicoResumoDto> ObterPorId(Guid id, CancellationToken ct);
}
