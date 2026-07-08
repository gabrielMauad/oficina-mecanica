using OrdensServico.Adapters.Models.ViewModels;
using OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;
using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Adapters.Presenters;

public static class OrdemServicoPresenter
{
    public static OrdemServicoViewModel Present(OrdemServico entity) =>
        new(
            entity.Id.Value,
            entity.ClienteId,
            entity.VeiculoId,
            entity.Status.ToString(),
            entity.DescricaoDiagnostico,
            entity.NotificadoEm,
            entity.EntregueEm,
            entity.CriadoEm,
            entity.AtualizadoEm,
            [.. entity.ItensServico.Select(x => new ItemServicoViewModel(x.ServicoId, x.Quantidade, x.PrecoUnitarioSnapshot))],
            [.. entity.ItensPeca.Select(x => new ItemPecaViewModel(x.PecaInsumoId, x.Quantidade, x.PrecoUnitarioSnapshot))],
            [.. entity.Orcamentos.Select(x => new OrcamentoViewModel(x.ValorTotal, x.Status.ToString(), x.DataGeracao, x.DataEnvio, x.DataAprovacao))]
        );

    public static StatusOrdemServicoViewModel PresentStatus(OrdemServico entity) =>
        new(entity.Id.Value, entity.Status.ToString());

    public static List<OrdemServicoViewModel> PresentListar(List<OrdemServicoListItem> readModels) =>
        [.. readModels.Select(x => new OrdemServicoViewModel(
            x.Id,
            x.ClienteId,
            x.VeiculoId,
            x.Status,
            x.DescricaoDiagnostico,
            x.NotificadoEm,
            x.EntregueEm,
            x.CriadoEm,
            x.AtualizadoEm,
            [.. x.ItensServico.Select(i => new ItemServicoViewModel(i.ServicoId, i.Quantidade, i.PrecoUnitarioSnapshot))],
            [.. x.ItensPeca.Select(i => new ItemPecaViewModel(i.PecaInsumoId, i.Quantidade, i.PrecoUnitarioSnapshot))],
            [.. x.Orcamentos.Select(o => new OrcamentoViewModel(o.ValorTotal, o.Status, o.DataGeracao, o.DataEnvio, o.DataAprovacao))]
        ))];
}
