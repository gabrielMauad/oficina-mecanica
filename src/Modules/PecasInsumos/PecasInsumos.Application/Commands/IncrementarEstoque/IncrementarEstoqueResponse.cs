using PecasInsumos.Domain;

namespace PecasInsumos.Application.Commands.IncrementarEstoque;

public sealed record IncrementarEstoqueResponse(
    Guid PecaInsumoId,
    string Nome,
    int QuantidadeEmEstoque,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static IncrementarEstoqueResponse FromPecaInsumo(PecaInsumo pecaInsumo)
    {
        return new IncrementarEstoqueResponse(
            pecaInsumo.Id.Value,
            pecaInsumo.Nome,
            pecaInsumo.QuantidadeEmEstoque,
            pecaInsumo.Ativo,
            pecaInsumo.CadastradoEm,
            pecaInsumo.AtualizadoEm
        );
    }

}

