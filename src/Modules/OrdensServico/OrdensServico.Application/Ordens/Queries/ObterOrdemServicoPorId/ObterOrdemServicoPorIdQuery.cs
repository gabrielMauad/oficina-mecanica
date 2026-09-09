using MediatR;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ObterOrdemServicoPorId;

public sealed record ObterOrdemServicoPorIdQuery(Guid OrdemServicoId, Guid? SolicitanteClienteId = null)
    : IRequest<Result<OrdemServico>>;
