namespace OrdemServico.Domain.OrdemServico;

public sealed record OrdemServicoId(Guid Value)
{
    public static OrdemServicoId Novo() => new(Guid.NewGuid());
}
