using Microsoft.EntityFrameworkCore;
using OrdensServico.Adapters.DataSources.Records;
using OrdensServico.Application.Ordens.Queries.ListarOrdensParaAcompanhamento;
using OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Infrastructure.Persistence;

namespace OrdensServico.Infrastructure.Queries;

internal sealed class ListarOrdensParaAcompanhamentoReadModelImpl : IListarOrdensParaAcompanhamentoReadModel
{
    private static readonly string[] StatusPorPrioridade =
    [
        nameof(StatusOrdemServico.EmExecucao),
        nameof(StatusOrdemServico.AguardandoAprovacao),
        nameof(StatusOrdemServico.EmDiagnostico),
        nameof(StatusOrdemServico.Recebida)
    ];

    private readonly OrdensServicoDbContext _context;

    public ListarOrdensParaAcompanhamentoReadModelImpl(OrdensServicoDbContext context) => _context = context;

    public async Task<List<OrdemServicoListItem>> Listar(CancellationToken ct = default)
    {
        var ordens = await _context.OrdensServico
            .AsNoTracking()
            .Include(o => o.ItensServico)
            .Include(o => o.ItensPeca)
            .Include(o => o.Orcamentos)
            .Where(o => StatusPorPrioridade.Contains(o.Status))
            .ToListAsync(ct);

        return ordens
            .OrderBy(o => Array.IndexOf(StatusPorPrioridade, o.Status))
            .ThenBy(o => o.CriadoEm)
            .Select(ToListItem)
            .ToList();
    }

    private static OrdemServicoListItem ToListItem(OrdemServicoRecord os) =>
        new(
            os.Id,
            os.ClienteId,
            os.VeiculoId,
            os.Status,
            os.DescricaoDiagnostico,
            os.NotificadoEm,
            os.EntregueEm,
            os.CriadoEm,
            os.AtualizadoEm,
            [.. os.ItensServico.Select(s => new ItemServicoListItem(s.ServicoId, s.Quantidade, s.PrecoUnitarioSnapshot))],
            [.. os.ItensPeca.Select(p => new ItemPecaListItem(p.PecaInsumoId, p.Quantidade, p.PrecoUnitarioSnapshot))],
            [.. os.Orcamentos.Select(oc => new OrcamentoListItem(oc.ValorTotal, oc.Status, oc.DataGeracao, oc.DataEnvio, oc.DataAprovacao))]
        );
}
