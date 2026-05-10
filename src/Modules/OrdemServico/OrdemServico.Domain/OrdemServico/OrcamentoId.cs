namespace OrdemServico.Domain.OrdemServico;

public sealed record OrcamentoId(Guid Value)
{
    public static OrcamentoId Novo() => new(Guid.NewGuid());
}
