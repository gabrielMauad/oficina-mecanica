namespace OrdensServico.Adapters.Models.ViewModels;

public sealed record OrcamentoViewModel(
    decimal ValorTotal,
    string Status,
    DateTime DataGeracao,
    DateTime? DataEnvio,
    DateTime? DataAprovacao);
