namespace OrdemServico.Domain.OrdemServico;

public sealed record ItemPecaId(Guid Value)
{
    public static ItemPecaId Novo() => new(Guid.NewGuid());
}
