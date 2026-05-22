using Cadastro.Contracts.Dtos;
using Cadastro.Contracts.Queries;
using OrdensServico.Domain.Ports;

namespace OrdensServico.Infrastructure.Acl;

internal sealed class VeiculoInfoAdapter : IVeiculoInfoPort
{
    private readonly ICadastroVeiculoQuery _cadastroVeiculoQuery;

    public VeiculoInfoAdapter(ICadastroVeiculoQuery cadastroVeiculoQuery) =>
        _cadastroVeiculoQuery = cadastroVeiculoQuery;

    public async Task<bool> ExisteEPertenceAoCliente(Guid veiculoId, Guid clienteId, CancellationToken ct)
    {
        VeiculoDto? veiculo = await _cadastroVeiculoQuery.ObterPorId(veiculoId, ct);

        return veiculo is not null && veiculo.ClienteId == clienteId;
    }

    public async Task<string?> ObterPlaca(Guid veiculoId, CancellationToken ct)
    {
        VeiculoDto? dto = await _cadastroVeiculoQuery.ObterPorId(veiculoId, ct);
        return dto?.Placa;
    }
}
