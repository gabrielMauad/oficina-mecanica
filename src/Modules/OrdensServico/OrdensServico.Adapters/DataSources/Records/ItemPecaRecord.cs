namespace OrdensServico.Adapters.DataSources.Records;

public sealed class ItemPecaRecord
{
    public Guid Id { get; set; }
    public Guid PecaInsumoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitarioSnapshot { get; set; }
}
