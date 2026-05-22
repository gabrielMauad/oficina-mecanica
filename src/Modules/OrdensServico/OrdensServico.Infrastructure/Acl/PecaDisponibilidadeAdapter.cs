using OrdensServico.Domain.Ports;
using OrdensServico.Domain.Ports.Dtos;
using PecasInsumos.Contracts.Queries;

namespace OrdensServico.Infrastructure.Acl;

internal sealed class PecaDisponibilidadeAdapter : IPecaDisponibilidadePort
{
    private readonly IPecasInsumosDisponibilidadeQuery _pecaDisponibilidadeQuery;

    public PecaDisponibilidadeAdapter(IPecasInsumosDisponibilidadeQuery pecaDisponibilidadeQuery) =>
        _pecaDisponibilidadeQuery = pecaDisponibilidadeQuery;

    public async Task<PecaDisponibilidade?> Verificar(Guid pecaInsumoId, int quantidade, CancellationToken ct)
    {
        var dto = await _pecaDisponibilidadeQuery.VerificarDisponibilidade(pecaInsumoId, quantidade, ct);
        return new PecaDisponibilidade(dto.Disponivel, dto.PrecoUnitario);
    }
}
