using Cadastro.Contracts.Dtos;
using Cadastro.Contracts.Queries;
using OrdensServico.Domain.Ports;

namespace OrdensServico.Infrastructure.Acl;

internal sealed class ServicoInfoAdapter : IServicoInfoPort
{
    private readonly ICadastroServicoQuery _cadastroServicoQuery;
    private readonly Dictionary<Guid, ServicoDto?> _cache = new();

    public ServicoInfoAdapter(ICadastroServicoQuery cadastroServicoQuery) =>
        _cadastroServicoQuery = cadastroServicoQuery;

    public async Task<decimal?> ObterPreco(Guid servicoId, CancellationToken ct)
    {
        var dto = await ObterDto(servicoId, ct);
        return dto?.PrecoBase;
    }

    public async Task<string?> ObterNome(Guid servicoId, CancellationToken ct)
    {
        var dto = await ObterDto(servicoId, ct);
        return dto?.Nome;
    }

    private async Task<ServicoDto?> ObterDto(Guid servicoId, CancellationToken ct)
    {
        if (!_cache.TryGetValue(servicoId, out var dto))
        {
            dto = await _cadastroServicoQuery.ObterPorId(servicoId, ct);
            _cache[servicoId] = dto;
        }
        return dto;
    }
}
