using PecasInsumos.Domain;

namespace PecasInsumos.Application.Commands.DesativarPecaInsumo;

public sealed record DesativarPecaInsumoResponse(
    Guid PecaInsumoId,
    string Nome,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static DesativarPecaInsumoResponse FromPecaInsumo(PecaInsumo pecaInsumo)
    {
        return new DesativarPecaInsumoResponse(
            pecaInsumo.Id.Value,
            pecaInsumo.Nome,
            pecaInsumo.Ativo,
            pecaInsumo.CadastradoEm,
            pecaInsumo.AtualizadoEm
        );
    }
}

