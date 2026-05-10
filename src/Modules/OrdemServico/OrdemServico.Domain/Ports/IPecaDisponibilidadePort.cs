using OrdemServico.Domain.Ports.Dtos;

namespace OrdemServico.Domain.Ports;

public interface IPecaDisponibilidadePort
{
    Task<PecaDisponibilidade?> Verificar(Guid pecaInsumoId, int quantidade, CancellationToken ct);
}
