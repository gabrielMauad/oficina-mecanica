namespace OrdensServico.Domain.Ports;

public interface IVeiculoInfoPort
{
    Task<bool> ExisteEPertenceAoCliente(Guid veiculoId, Guid clienteId, CancellationToken ct);
    Task<string?> ObterPlaca(Guid veiculoId, CancellationToken ct);
}
