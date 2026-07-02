namespace OrdensServico.Adapters.Models.ViewModels;

public sealed record OrdemServicoViewModel(
    Guid Id,
    Guid ClienteId,
    Guid VeiculoId,
    string Status,
    string? DescricaoDiagnostico,
    DateTime? NotificadoEm,
    DateTime? EntregueEm,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    IReadOnlyList<ItemServicoViewModel> ItensServico,
    IReadOnlyList<ItemPecaViewModel> ItensPeca,
    IReadOnlyList<OrcamentoViewModel> Orcamentos);
