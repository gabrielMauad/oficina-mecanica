using OrdensServico.Application.Gateways.Dtos;

namespace OrdensServico.Application.Gateways;

public interface IPecaInsumoInfoGateway
{
    Task<PecaInsumoInfo?> Obter(Guid pecaInsumoId, CancellationToken ct);
}
