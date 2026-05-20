using MediatR;
using OrdensServico.Contracts.Dtos;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ObterOrdemServicoPorId;

public sealed record ObterOrdemServicoPorIdQuery(Guid OrdemServicoId) : IRequest<Result<OrdemServicoResumoDto>>;
