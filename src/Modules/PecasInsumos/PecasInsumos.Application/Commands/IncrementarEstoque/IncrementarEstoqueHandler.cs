using MediatR;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.IncrementarEstoque;

public sealed class IncrementarEstoqueHandler : IRequestHandler<IncrementarEstoqueCommand, Result<IncrementarEstoqueResponse>>
{
    private readonly IPecaInsumoRepository _repository;

    public IncrementarEstoqueHandler(
        IPecaInsumoRepository repository
    ) => _repository = repository;


    public async Task<Result<IncrementarEstoqueResponse>> Handle(IncrementarEstoqueCommand command, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(command.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _repository.ObterPorId(pecaInsumoId, cancellationToken);

        if (pecaInsumo == null)
            return PecaInsumoErrors.NaoEncontrada;
        if (!pecaInsumo.Ativo)
            return PecaInsumoErrors.JaDesativado;

        Result<PecaInsumo> pecaInsumoResult = pecaInsumo.Incrementar(command.Quantidade);
        if (pecaInsumoResult.IsFailure)
            return pecaInsumoResult.Error;

        pecaInsumo = pecaInsumoResult.Value;

        await _repository.Atualizar(pecaInsumo, cancellationToken);

        return IncrementarEstoqueResponse.FromPecaInsumo(pecaInsumo);
    }
}

