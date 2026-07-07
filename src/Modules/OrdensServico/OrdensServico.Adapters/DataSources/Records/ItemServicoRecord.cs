namespace OrdensServico.Adapters.DataSources.Records;

public sealed class ItemServicoRecord
{
    public Guid Id { get; set; }
    public Guid ServicoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitarioSnapshot { get; set; }
}
