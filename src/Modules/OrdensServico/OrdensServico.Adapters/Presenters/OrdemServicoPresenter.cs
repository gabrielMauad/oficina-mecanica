using OrdensServico.Adapters.Models.ViewModels;
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
}
