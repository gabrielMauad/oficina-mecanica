using PecasInsumos.Domain;

namespace PecasInsumos.Application.Commands.DecrementarEstoque;

public sealed record DecrementarEstoqueResponse(
 Guid PecaInsumoId,
 string Nome,
 int QuantidadeEmEstoque,
 bool Ativo,
 DateTime CadastradoEm,
 DateTime AtualizadoEm
)
{
    public static DecrementarEstoqueResponse FromPecaInsumo(PecaInsumo pecaInsumo)
    {
        return new DecrementarEstoqueResponse(
            pecaInsumo.Id.Value,
            pecaInsumo.Nome,
            pecaInsumo.QuantidadeEmEstoque,
            pecaInsumo.Ativo,
            pecaInsumo.CadastradoEm,
            pecaInsumo.AtualizadoEm
        );
    }

}

