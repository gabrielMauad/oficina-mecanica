using SharedKernel.Domain;

namespace OrdensServico.Domain.OrdemServico;

public sealed class ItemServico : Entity<ItemServicoId>
{
    public Guid ServicoId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitarioSnapshot { get; private set; }

    private ItemServico(ItemServicoId id, Guid servicoId, int quantidade, decimal precoUnitarioSnapshot)
        : base(id)
    {
        ServicoId = servicoId;
        Quantidade = quantidade;
        PrecoUnitarioSnapshot = precoUnitarioSnapshot;
    }

    internal static Result<ItemServico> Criar(Guid servicoId, int quantidade, decimal precoUnitarioSnapshot)
    {
        if (servicoId == Guid.Empty)
            return Error.Validation("ItemServico.ServicoIdVazio", "ServicoId é obrigatório.");
        if (quantidade <= 0)
            return Error.Validation("ItemServico.QuantidadeInvalida", "Quantidade deve ser maior que zero.");
        if (precoUnitarioSnapshot < 0)
            return Error.Validation("ItemServico.PrecoUnitarioSnapshotInvalido", "Preço unitário snapshot não pode ser negativo.");

        var itemServico = new ItemServico(ItemServicoId.Novo(), servicoId, quantidade, precoUnitarioSnapshot);
        return itemServico;
    }

    internal Result<ItemServico> AtualizarQuantidade(int novaQuantidade)
    {
        if (novaQuantidade <= 0)
            return Error.Validation("ItemServico.QuantidadeInvalida", "Quantidade deve ser maior que zero.");

        Quantidade = novaQuantidade;
        return this;
    }

    internal Result<ItemServico> AtualizarPrecoUnitarioSnapshot(decimal novoPrecoUnitario)
    {
        if (novoPrecoUnitario < 0)
            return Error.Validation("ItemServico.PrecoUnitarioInvalido", "Preço unitário não pode ser negativo.");
        PrecoUnitarioSnapshot = novoPrecoUnitario;
        return this;
    }
}
