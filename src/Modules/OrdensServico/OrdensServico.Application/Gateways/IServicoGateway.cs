namespace OrdensServico.Application.Gateways;

public interface IServicoGateway
{
    Task<decimal?> ObterPreco(Guid servicoId, CancellationToken ct);
    Task<string?> ObterNome(Guid servicoId, CancellationToken ct);
}
