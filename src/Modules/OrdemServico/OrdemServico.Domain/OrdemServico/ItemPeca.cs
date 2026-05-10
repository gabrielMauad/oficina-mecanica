using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico;

public sealed class ItemPeca : Entity<ItemPecaId>
{
    public Guid PecaInsumoId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitarioSnapshot { get; private set; }

    private ItemPeca(ItemPecaId id, Guid pecaInsumoId, int quantidade, decimal precoUnitarioSnapshot)
        : base(id)
    {
        PecaInsumoId = pecaInsumoId;
        Quantidade = quantidade;
        PrecoUnitarioSnapshot = precoUnitarioSnapshot;
    }

    internal static Result<ItemPeca> Criar(Guid pecaInsumoId, int quantidade, decimal precoUnitarioSnapshot)
    {
        if (pecaInsumoId == Guid.Empty)
            return Error.Validation("ItemPeca.PecaInsumoIdVazio", "PecaInsumoId é obrigatório.");
        if (quantidade <= 0)
            return Error.Validation("ItemPeca.QuantidadeInvalida", "Quantidade deve ser maior que zero.");
        if (precoUnitarioSnapshot < 0)
            return Error.Validation("ItemPeca.PrecoUnitarioSnapshotInvalido", "Preço unitário snapshot não pode ser negativo.");

        var itemPeca = new ItemPeca(ItemPecaId.Novo(), pecaInsumoId, quantidade, precoUnitarioSnapshot);
        return itemPeca;
    }

    internal Result<ItemPeca> AtualizarQuantidade(int novaQuantidade)
    {
        if (novaQuantidade <= 0)
            return Error.Validation("ItemPeca.QuantidadeInvalida", "Quantidade deve ser maior que zero.");

        Quantidade = novaQuantidade;
        return this;
    }

    internal Result<ItemPeca> AtualizarPrecoUnitarioSnapshot(decimal novoPrecoUnitario)
    {
        if (novoPrecoUnitario < 0)
            return Error.Validation("ItemPeca.PrecoUnitarioInvalido", "Preço unitário não pode ser negativo.");
        PrecoUnitarioSnapshot = novoPrecoUnitario;
        return this;
    }
}

