using PecasInsumos.Domain;

namespace PecasInsumos.Application.Commands.AtualizarPecaInsumo;

public sealed record AtualizarPecaInsumoResponse(
    Guid PecaInsumoId,
    decimal PrecoUnitario,
    string? Descricao,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static AtualizarPecaInsumoResponse FromPecaInsumo(PecaInsumo pecaInsumo)
    {
        return new AtualizarPecaInsumoResponse(
            pecaInsumo.Id.Value,
            pecaInsumo.PrecoUnitario.Valor,
            pecaInsumo.Descricao,
            pecaInsumo.Ativo,
            pecaInsumo.CadastradoEm,
            pecaInsumo.AtualizadoEm
        );
    }
}

