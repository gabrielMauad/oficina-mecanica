using SharedKernel.Application;

namespace PecasInsumos.Application.Commands.AtualizarPecaInsumo;

public sealed record AtualizarPecaInsumoCommand(
    Guid PecaInsumoId,
    decimal? PrecoUnitario,
    string? Descricao
) : ICommand<AtualizarPecaInsumoResponse>;
