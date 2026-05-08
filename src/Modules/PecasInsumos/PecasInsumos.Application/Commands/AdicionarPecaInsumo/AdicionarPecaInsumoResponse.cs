using PecasInsumos.Domain;

namespace PecasInsumos.Application.Commands.AdicionarPecaInsumo;

public sealed record AdicionarPecaInsumoResponse(
    Guid PecaInsumoId,
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
    public static AdicionarPecaInsumoResponse FromPecaInsumo(PecaInsumo pecaInsumo)
    {
        return new AdicionarPecaInsumoResponse(
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

