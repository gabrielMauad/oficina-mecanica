using OrdensServico.Adapters.DataSources;
using OrdensServico.Adapters.DataSources.Mappers;
using OrdensServico.Application.Gateways;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Application;

namespace OrdensServico.Adapters.Gateways;

public sealed class OrdemServicoGateway : IOrdemServicoGateway
{
    private readonly IOrdemServicoRepository _repository;
    private readonly IDomainEventCollector _collector;

    public OrdemServicoGateway(IOrdemServicoRepository repository, IDomainEventCollector collector)
    {
        _repository = repository;
        _collector = collector;
    }

    public Task Adicionar(OrdemServico ordemServico, CancellationToken ct = default)
    {
        _collector.Registrar(ordemServico);
        return _repository.Adicionar(OrdemServicoMapper.ToRecord(ordemServico), ct);
    }

    public async Task<OrdemServico?> ObterPorId(OrdemServicoId id, CancellationToken ct = default)
    {
        var record = await _repository.ObterPorId(id.Value, ct);
        return record is null ? null : OrdemServicoMapper.ToDomain(record);
    }

    public Task Atualizar(OrdemServico ordemServico, CancellationToken ct = default)
    {
        _collector.Registrar(ordemServico);
        return _repository.Atualizar(OrdemServicoMapper.ToRecord(ordemServico), ct);
    }
}
