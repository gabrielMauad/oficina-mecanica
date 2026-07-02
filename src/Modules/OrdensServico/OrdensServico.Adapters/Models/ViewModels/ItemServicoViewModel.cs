namespace OrdensServico.Adapters.Models.ViewModels;

public sealed record ItemServicoViewModel(
    Guid ServicoId,
    int Quantidade,
    decimal PrecoUnitarioSnapshot);
