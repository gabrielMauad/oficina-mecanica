using MediatR;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.DesativarPecaInsumo;

public sealed class DesativarPecaInsumoHandler : IRequestHandler<DesativarPecaInsumoCommand, Result<DesativarPecaInsumoResponse>>
{
    private readonly IPecaInsumoRepository _repository;

    public DesativarPecaInsumoHandler(IPecaInsumoRepository repository) => _repository = repository;

    public async Task<Result<DesativarPecaInsumoResponse>> Handle(DesativarPecaInsumoCommand command, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(command.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _repository.ObterPorId(pecaInsumoId, cancellationToken);
        if (pecaInsumo is null)
            return PecaInsumoErrors.NaoEncontrado;
        if (!pecaInsumo.Ativo)
            return PecaInsumoErrors.JaDesativado;
        pecaInsumo.Desativar();
        await _repository.Atualizar(pecaInsumo, cancellationToken);

        return DesativarPecaInsumoResponse.FromPecaInsumo(pecaInsumo);
    }
}

