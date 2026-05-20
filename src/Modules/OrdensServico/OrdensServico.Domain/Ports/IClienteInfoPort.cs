using OrdensServico.Domain.Ports.Dtos;

namespace OrdensServico.Domain.Ports;

public interface IClienteInfoPort
{
    Task<bool> ExisteEAtivo(Guid clienteId, CancellationToken ct);
    Task<ClienteInfo?> ObterInfo(Guid clienteId, CancellationToken ct);
}
