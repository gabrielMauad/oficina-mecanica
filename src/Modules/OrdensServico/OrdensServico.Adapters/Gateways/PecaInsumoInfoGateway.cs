using OrdensServico.Application.Gateways;
using OrdensServico.Application.Gateways.Dtos;
using PecasInsumos.Contracts.Dtos;
using PecasInsumos.Contracts.Queries;

namespace OrdensServico.Adapters.Gateways;

public sealed class PecaInsumoInfoGateway : IPecaInsumoInfoGateway
{
    private readonly IPecaInsumoQuery _pecaInsumoQuery;

    public PecaInsumoInfoGateway(IPecaInsumoQuery pecaInsumoQuery) =>
        _pecaInsumoQuery = pecaInsumoQuery;

    public async Task<PecaInsumoInfo?> Obter(Guid pecaInsumoId, CancellationToken ct)
    {
        PecaInsumoResumoDto? dto = await _pecaInsumoQuery.ObterPorId(pecaInsumoId, ct);
        if (dto == null) return null;
        return new PecaInsumoInfo(dto.Nome, dto.UnidadeDeMedida);
    }
}
