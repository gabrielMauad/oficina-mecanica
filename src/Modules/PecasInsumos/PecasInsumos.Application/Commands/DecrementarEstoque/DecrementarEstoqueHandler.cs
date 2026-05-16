using MediatR;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.DecrementarEstoque;

public sealed class DecrementarEstoqueHandler : IRequestHandler<DecrementarEstoqueCommand, Result<DecrementarEstoqueResponse>>
{
    private readonly IPecaInsumoRepository _repository;

    public DecrementarEstoqueHandler(IPecaInsumoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DecrementarEstoqueResponse>> Handle(DecrementarEstoqueCommand command, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(command.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _repository.ObterPorId(pecaInsumoId, cancellationToken);

        if (pecaInsumo == null)
            return PecaInsumoErrors.NaoEncontrado;
        if (!pecaInsumo.Ativo)
            return PecaInsumoErrors.JaDesativado;

        Result<PecaInsumo> pecaInsumoResult = pecaInsumo.Decrementar(command.Quantidade);
        if (pecaInsumoResult.IsFailure)
            return pecaInsumoResult.Error;

        pecaInsumo = pecaInsumoResult.Value;

        await _repository.Atualizar(pecaInsumo, cancellationToken);

        return DecrementarEstoqueResponse.FromPecaInsumo(pecaInsumo);
    }
}
