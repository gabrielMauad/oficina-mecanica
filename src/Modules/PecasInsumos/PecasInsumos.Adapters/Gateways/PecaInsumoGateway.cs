using PecasInsumos.Adapters.DataSources;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;

namespace PecasInsumos.Adapters.Gateways;

public sealed class PecaInsumoGateway : IPecaInsumoGateway
{
    private readonly IPecaInsumoRepository _repository;

    public PecaInsumoGateway(IPecaInsumoRepository repository) => _repository = repository;

    public Task Adicionar(PecaInsumo pecaInsumo, CancellationToken ct = default) =>
        _repository.Adicionar(pecaInsumo, ct);

    public Task<PecaInsumo?> ObterPorId(PecaInsumoId id, CancellationToken ct = default) =>
        _repository.ObterPorId(id, ct);

    public Task<bool> ExistePorNome(string nome, CancellationToken ct = default) =>
        _repository.ExistePorNome(nome, ct);

    public Task Atualizar(PecaInsumo pecaInsumo, CancellationToken ct = default) =>
        _repository.Atualizar(pecaInsumo, ct);
}
