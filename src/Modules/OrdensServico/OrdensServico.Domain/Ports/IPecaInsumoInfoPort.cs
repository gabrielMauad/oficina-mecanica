using OrdensServico.Domain.Ports.Dtos;

namespace OrdensServico.Domain.Ports;

public interface IPecaInsumoInfoPort
{
    Task<PecaInsumoInfo?> Obter(Guid pecaInsumoId, CancellationToken ct);
}
