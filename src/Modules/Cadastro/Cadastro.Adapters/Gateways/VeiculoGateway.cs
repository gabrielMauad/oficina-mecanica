using Cadastro.Adapters.DataSources;
using Cadastro.Application.Gateways;
using Cadastro.Domain.Veiculo;

namespace Cadastro.Adapters.Gateways;

public sealed class VeiculoGateway : IVeiculoGateway
{
    private readonly IVeiculoRepository _repository;

    public VeiculoGateway(IVeiculoRepository repository) => _repository = repository;

    public Task Adicionar(Veiculo veiculo, CancellationToken ct = default) =>
        _repository.Adicionar(veiculo, ct);

    public Task<Veiculo?> ObterPorId(VeiculoId id, CancellationToken ct = default) =>
        _repository.ObterPorId(id, ct);

    public Task<bool> ExistePorPlaca(string placa, CancellationToken ct = default) =>
        _repository.ExistePorPlaca(placa, ct);
}
