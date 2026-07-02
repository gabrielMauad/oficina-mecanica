using Cadastro.Adapters.DataSources;
using Cadastro.Application.Gateways;
using Cadastro.Domain.Cliente;

namespace Cadastro.Adapters.Gateways;

public sealed class ClienteGateway : IClienteGateway
{
    private readonly IClienteRepository _repository;

    public ClienteGateway(IClienteRepository repository) => _repository = repository;

    public Task Adicionar(Cliente cliente, CancellationToken ct = default) =>
        _repository.Adicionar(cliente, ct);

    public Task<Cliente?> ObterPorId(ClienteId id, CancellationToken ct = default) =>
        _repository.ObterPorId(id, ct);

    public Task<bool> ExistePorDocumento(string documento, CancellationToken ct = default) =>
        _repository.ExistePorDocumento(documento, ct);

    public Task Atualizar(Cliente cliente, CancellationToken ct = default) =>
        _repository.Atualizar(cliente, ct);
}
