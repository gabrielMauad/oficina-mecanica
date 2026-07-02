using Microsoft.EntityFrameworkCore;
using OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Infrastructure.Persistence;

namespace OrdensServico.Infrastructure.Queries;

internal sealed class ListarOrdensPorClienteReadModelImpl : IListarOrdensPorClienteReadModel
{
    private readonly OrdensServicoDbContext _context;

    public ListarOrdensPorClienteReadModelImpl(OrdensServicoDbContext context) => _context = context;

    public async Task<List<OrdemServicoListItem>> Listar(Guid clienteId, CancellationToken ct = default)
    {
        var ordens = await _context.OrdensServico
            .AsNoTracking()
            .Include(o => o.ItensServico)
            .Include(o => o.ItensPeca)
            .Include(o => o.Orcamentos)
            .Where(o => o.ClienteId == clienteId)
            .ToListAsync(ct);

        return ordens.Select(ToListItem).ToList();
    }

    private static OrdemServicoListItem ToListItem(OrdemServico os) =>
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
            [.. os.ItensServico.Select(s => new ItemServicoListItem(s.ServicoId, s.Quantidade, s.PrecoUnitarioSnapshot))],
            [.. os.ItensPeca.Select(p => new ItemPecaListItem(p.PecaInsumoId, p.Quantidade, p.PrecoUnitarioSnapshot))],
            [.. os.Orcamentos.Select(oc => new OrcamentoListItem(oc.ValorTotal, oc.Status.ToString(), oc.DataGeracao, oc.DataEnvio, oc.DataAprovacao))]
        );
}
