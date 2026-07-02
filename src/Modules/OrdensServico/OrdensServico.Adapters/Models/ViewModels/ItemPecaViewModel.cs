namespace OrdensServico.Adapters.Models.ViewModels;

public sealed record ItemPecaViewModel(
    Guid PecaInsumoId,
    int Quantidade,
    decimal PrecoUnitarioSnapshot);
