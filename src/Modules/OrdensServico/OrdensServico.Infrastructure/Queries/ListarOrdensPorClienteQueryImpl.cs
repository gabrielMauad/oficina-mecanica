using Microsoft.EntityFrameworkCore;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Contracts.Queries;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Infrastructure.Persistence;

namespace OrdensServico.Infrastructure.Queries;

internal sealed class ListarOrdensPorClienteQueryImpl : IListarOrdensPorClienteQuery
{
    private readonly OrdensServicoDbContext _context;

    public ListarOrdensPorClienteQueryImpl(OrdensServicoDbContext context) => _context = context;

    public async Task<IReadOnlyList<OrdemServicoResumoDto>> Listar(Guid clienteId, CancellationToken ct = default)
    {
        var ordens = await _context.OrdensServico
            .AsNoTracking()
            .Include(o => o.ItensServico)
            .Include(o => o.ItensPeca)
            .Include(o => o.Orcamentos)
            .Where(o => o.ClienteId == clienteId)
            .ToListAsync(ct);

        return ordens.Select(ToDto).ToList();
    }

    private static OrdemServicoResumoDto ToDto(OrdemServico os) =>
        new(
            os.Id.Value,
            os.ClienteId,
            os.VeiculoId,
            os.Status.ToString(),
            os.DescricaoDiagnostico,
            os.NotificadoEm,
            os.EntregueEm,
            os.CriadoEm,
            os.AtualizadoEm,
            [.. os.ItensServico.Select(s => new ItemServicoDto(s.ServicoId, s.Quantidade, s.PrecoUnitarioSnapshot))],
            [.. os.ItensPeca.Select(p => new ItemPecaDto(p.PecaInsumoId, p.Quantidade, p.PrecoUnitarioSnapshot))],
            [.. os.Orcamentos.Select(oc => new OrcamentoDto(oc.ValorTotal, oc.Status.ToString(), oc.DataGeracao, oc.DataEnvio, oc.DataAprovacao))]
        );
}
