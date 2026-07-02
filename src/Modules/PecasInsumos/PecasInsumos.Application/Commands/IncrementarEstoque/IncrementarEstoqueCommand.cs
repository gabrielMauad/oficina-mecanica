using SharedKernel.Application;

namespace PecasInsumos.Application.Commands.IncrementarEstoque;

public sealed record IncrementarEstoqueCommand(
    Guid PecaInsumoId,
    int Quantidade
) : ICommand<IncrementarEstoqueResponse>;
