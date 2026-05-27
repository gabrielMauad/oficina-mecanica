using OrdensServico.Domain.Ports;
using OrdensServico.Domain.Ports.Dtos;
using PecasInsumos.Contracts.Dtos;
using PecasInsumos.Contracts.Queries;

namespace OrdensServico.Infrastructure.Acl;

internal sealed class PecaInsumoInfoAdapter : IPecaInsumoInfoPort
{
    private readonly IPecaInsumoQuery _pecaInsumoQuery;

    public PecaInsumoInfoAdapter(IPecaInsumoQuery pecaInsumoQuery) =>
        _pecaInsumoQuery = pecaInsumoQuery;

    public async Task<PecaInsumoInfo?> Obter(Guid pecaInsumoId, CancellationToken ct)
    {
        PecaInsumoResumoDto? dto = await _pecaInsumoQuery.ObterPorId(pecaInsumoId, ct);
        if (dto == null) return null;
        return new PecaInsumoInfo(dto.Nome, dto.UnidadeDeMedida);
    }
}
