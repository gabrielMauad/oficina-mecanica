namespace OrdensServico.Domain.Ports;

public interface IServicoInfoPort
{
    Task<decimal?> ObterPreco(Guid servicoId, CancellationToken ct);
}
