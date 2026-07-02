using MediatR;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.AtualizarPecaInsumo;

public sealed class AtualizarPecaInsumoHandler : IRequestHandler<AtualizarPecaInsumoCommand, Result<AtualizarPecaInsumoResponse>>
{
    private readonly IPecaInsumoRepository _repository;
    public AtualizarPecaInsumoHandler(IPecaInsumoRepository repository) => _repository = repository;
    public async Task<Result<AtualizarPecaInsumoResponse>> Handle(AtualizarPecaInsumoCommand command, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(command.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _repository.ObterPorId(pecaInsumoId, cancellationToken);
        if (pecaInsumo is null)
            return PecaInsumoErrors.NaoEncontrada;
        if (!pecaInsumo.Ativo)
            return PecaInsumoErrors.JaDesativado;

        bool hasChanges = false;
        if (command.PrecoUnitario is not null && command.PrecoUnitario != pecaInsumo.PrecoUnitario.Valor)
        {
            Result<PecaInsumo> atualizarPrecoResult = pecaInsumo.AtualizarPrecoUnitario(command.PrecoUnitario.Value);
            if (atualizarPrecoResult.IsFailure)
                return atualizarPrecoResult.Error;
            hasChanges = true;
        }
        if (command.Descricao is not null && command.Descricao != pecaInsumo.Descricao)
        {
            Result<PecaInsumo> atualizarDescricaoResult = pecaInsumo.AtualizarDescricao(command.Descricao);
            if (atualizarDescricaoResult.IsFailure)
                return atualizarDescricaoResult.Error;
            hasChanges = true;
        }

        if (hasChanges)
            await _repository.Atualizar(pecaInsumo, cancellationToken);

        return AtualizarPecaInsumoResponse.FromPecaInsumo(pecaInsumo);
    }
}

