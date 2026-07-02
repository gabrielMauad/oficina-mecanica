using Cadastro.Contracts.Queries;
using OrdensServico.Application.Gateways;
using OrdensServico.Application.Gateways.Dtos;

namespace OrdensServico.Adapters.Gateways;

public sealed class ClienteGateway : IClienteGateway
{
    private readonly ICadastroClienteQuery _cadastroClienteQuery;

    public ClienteGateway(ICadastroClienteQuery cadastroClienteQuery)
        => _cadastroClienteQuery = cadastroClienteQuery;

    public async Task<bool> ExisteEAtivo(Guid clienteId, CancellationToken ct)
    {
        var dto = await _cadastroClienteQuery.ObterPorId(clienteId, ct);
        return dto is { Ativo: true };
    }

    public async Task<ClienteInfo?> ObterInfo(Guid clienteId, CancellationToken ct)
    {
        var dto = await _cadastroClienteQuery.ObterPorId(clienteId, ct);
        return dto is null ? null : new ClienteInfo(dto.Nome, dto.Email);
    }
}
