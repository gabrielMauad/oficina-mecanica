using Cadastro.Adapters.DataSources;
using Cadastro.Application.Gateways;
using Cadastro.Domain.Servico;

namespace Cadastro.Adapters.Gateways;

public sealed class ServicoGateway : IServicoGateway
{
    private readonly IServicoRepository _repository;

    public ServicoGateway(IServicoRepository repository) => _repository = repository;

    public Task Adicionar(Servico servico, CancellationToken ct = default) =>
        _repository.Adicionar(servico, ct);

    public Task<Servico?> ObterPorId(ServicoId id, CancellationToken ct = default) =>
        _repository.ObterPorId(id, ct);

    public Task<bool> ExistePorNome(string nome, CancellationToken ct = default) =>
        _repository.ExistePorNome(nome, ct);

    public Task Atualizar(Servico servico, CancellationToken ct = default) =>
        _repository.Atualizar(servico, ct);
}
