using OrdensServico.Adapters.DataSources;
using OrdensServico.Application.Gateways;
using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Adapters.Gateways;

public sealed class OrdemServicoGateway : IOrdemServicoGateway
{
    private readonly IOrdemServicoRepository _repository;

    public OrdemServicoGateway(IOrdemServicoRepository repository) => _repository = repository;

    public Task Adicionar(OrdemServico ordemServico, CancellationToken ct = default) =>
        _repository.Adicionar(ordemServico, ct);

    public Task<OrdemServico?> ObterPorId(OrdemServicoId id, CancellationToken ct = default) =>
        _repository.ObterPorId(id, ct);

    public Task Atualizar(OrdemServico ordemServico, CancellationToken ct = default) =>
        _repository.Atualizar(ordemServico, ct);
}
