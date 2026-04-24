using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Servicos.Queries.ObterServicoPorId;

public sealed record ObterServicoPorIdQuery(Guid ServicoId) : IRequest<Result<ObterServicoPorIdResponse>>;

