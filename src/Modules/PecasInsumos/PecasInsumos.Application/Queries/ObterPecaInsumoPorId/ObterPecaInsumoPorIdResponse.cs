using PecasInsumos.Domain;

namespace PecasInsumos.Application.Queries.ObterPecaInsumoPorId;

public sealed record ObterPecaInsumoPorIdResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal PrecoUnitario,
    int QuantidadeEmEstoque,
    string UnidadeDeMedida,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static ObterPecaInsumoPorIdResponse FromPecaInsumo(PecaInsumo pecaInsumo)
    {
        return new ObterPecaInsumoPorIdResponse(
            pecaInsumo.Id.Value,
            pecaInsumo.Nome,
            pecaInsumo.Descricao,
            pecaInsumo.PrecoUnitario.Valor,
            pecaInsumo.QuantidadeEmEstoque,
            pecaInsumo.UnidadeDeMedida.ToString(),
            pecaInsumo.Ativo,
            pecaInsumo.CadastradoEm,
            pecaInsumo.AtualizadoEm
        );
    }
}

