namespace OrdensServico.Domain.OrdemServico;

public sealed record ItemServicoId(Guid Value)
{
    public static ItemServicoId Novo() => new(Guid.NewGuid());
}
