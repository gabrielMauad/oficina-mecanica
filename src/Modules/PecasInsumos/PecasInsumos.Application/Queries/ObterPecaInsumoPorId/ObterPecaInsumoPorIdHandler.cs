using MediatR;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Queries.ObterPecaInsumoPorId;

public sealed class ObterPecaInsumoPorIdHandler : IRequestHandler<ObterPecaInsumoPorIdQuery, Result<ObterPecaInsumoPorIdResponse>>
{
    private readonly IPecaInsumoRepository _repository;

    public ObterPecaInsumoPorIdHandler(IPecaInsumoRepository repository) => _repository = repository;

    public async Task<Result<ObterPecaInsumoPorIdResponse>> Handle(ObterPecaInsumoPorIdQuery request, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(request.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _repository.ObterPorId(pecaInsumoId, cancellationToken);
        if (pecaInsumo is null)
            return PecaInsumoErrors.NaoEncontrado;
        return ObterPecaInsumoPorIdResponse.FromPecaInsumo(pecaInsumo);
    }
}

