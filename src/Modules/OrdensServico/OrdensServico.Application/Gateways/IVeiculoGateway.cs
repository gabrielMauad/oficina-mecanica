namespace OrdensServico.Application.Gateways;

public interface IVeiculoGateway
{
    Task<bool> ExisteEPertenceAoCliente(Guid veiculoId, Guid clienteId, CancellationToken ct);
    Task<string?> ObterPlaca(Guid veiculoId, CancellationToken ct);
}
