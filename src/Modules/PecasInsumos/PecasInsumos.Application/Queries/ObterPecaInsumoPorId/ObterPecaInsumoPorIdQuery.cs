using MediatR;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Queries.ObterPecaInsumoPorId;

public sealed record ObterPecaInsumoPorIdQuery(Guid PecaInsumoId) : IRequest<Result<ObterPecaInsumoPorIdResponse>>;
