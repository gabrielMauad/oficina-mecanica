using OrdensServico.Domain.Ports.Dtos;

namespace OrdensServico.Domain.Ports;

public interface IPecaDisponibilidadePort
{
    Task<PecaDisponibilidade?> Verificar(Guid pecaInsumoId, int quantidade, CancellationToken ct);
}
