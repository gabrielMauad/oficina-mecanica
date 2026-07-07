namespace OrdensServico.Adapters.DataSources.Records;

public sealed class OrcamentoRecord
{
    public Guid Id { get; set; }
    public decimal ValorTotal { get; set; }
    public string Status { get; set; } = "";
    public DateTime DataGeracao { get; set; }
    public DateTime? DataEnvio { get; set; }
    public DateTime? DataAprovacao { get; set; }
}
