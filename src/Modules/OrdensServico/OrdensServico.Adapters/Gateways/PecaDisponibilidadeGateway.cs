using OrdensServico.Application.Gateways;
using OrdensServico.Application.Gateways.Dtos;
using PecasInsumos.Contracts.Queries;

namespace OrdensServico.Adapters.Gateways;

public sealed class PecaDisponibilidadeGateway : IPecaDisponibilidadeGateway
{
    private readonly IPecasInsumosDisponibilidadeQuery _pecaDisponibilidadeQuery;

    public PecaDisponibilidadeGateway(IPecasInsumosDisponibilidadeQuery pecaDisponibilidadeQuery) =>
        _pecaDisponibilidadeQuery = pecaDisponibilidadeQuery;

    public async Task<PecaDisponibilidade?> Verificar(Guid pecaInsumoId, int quantidade, CancellationToken ct)
    {
        var dto = await _pecaDisponibilidadeQuery.VerificarDisponibilidade(pecaInsumoId, quantidade, ct);
        return new PecaDisponibilidade(dto.Disponivel, dto.PrecoUnitario);
    }
}
