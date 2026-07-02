using MediatR;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ObterOrdemServicoPorId;

public sealed record ObterOrdemServicoPorIdQuery(Guid OrdemServicoId) : IRequest<Result<OrdemServico>>;
