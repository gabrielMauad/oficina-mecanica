using Cadastro.Contracts.Queries;
using OrdensServico.Domain.Ports;
using OrdensServico.Domain.Ports.Dtos;

namespace OrdensServico.Infrastructure.Acl;

internal sealed class ClienteInfoAdapter : IClienteInfoPort
{
    private readonly ICadastroClienteQuery _cadastroClienteQuery;

    public ClienteInfoAdapter(ICadastroClienteQuery cadastroClienteQuery)
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
