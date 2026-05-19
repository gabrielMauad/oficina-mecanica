namespace OrdensServico.Domain.Ports;

public interface IClienteInfoPort
{
    Task<bool> ExisteEAtivo(Guid clienteId, CancellationToken ct);
}
