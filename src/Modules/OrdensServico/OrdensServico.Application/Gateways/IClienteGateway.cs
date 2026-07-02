using OrdensServico.Application.Gateways.Dtos;

namespace OrdensServico.Application.Gateways;

public interface IClienteGateway
{
    Task<bool> ExisteEAtivo(Guid clienteId, CancellationToken ct);
    Task<ClienteInfo?> ObterInfo(Guid clienteId, CancellationToken ct);
}
