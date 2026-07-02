using OrdensServico.Application.Gateways.Dtos;

namespace OrdensServico.Application.Gateways;

public interface IPecaDisponibilidadeGateway
{
    Task<PecaDisponibilidade?> Verificar(Guid pecaInsumoId, int quantidade, CancellationToken ct);
}
