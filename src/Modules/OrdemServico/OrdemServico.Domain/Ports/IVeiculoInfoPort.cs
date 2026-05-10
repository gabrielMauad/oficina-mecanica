namespace OrdemServico.Domain.Ports;

public interface IVeiculoInfoPort
{
    Task<bool> ExisteEPertenceAoCliente(Guid veiculoId, Guid clienteId, CancellationToken ct);
}
