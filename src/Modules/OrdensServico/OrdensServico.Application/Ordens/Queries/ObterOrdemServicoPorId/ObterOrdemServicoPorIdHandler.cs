using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ObterOrdemServicoPorId;

public sealed class ObterOrdemServicoPorIdHandler : IRequestHandler<ObterOrdemServicoPorIdQuery, Result<OrdemServico>>
{
    private readonly IOrdemServicoGateway _ordemServicoGateway;

    public ObterOrdemServicoPorIdHandler(IOrdemServicoGateway ordemServicoGateway) =>
        _ordemServicoGateway = ordemServicoGateway;

    public async Task<Result<OrdemServico>> Handle(ObterOrdemServicoPorIdQuery request, CancellationToken ct)
    {
        var ordemServico = await _ordemServicoGateway.ObterPorId(
            new OrdemServicoId(request.OrdemServicoId),
            ct
        );

        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        return ordemServico;
    }
}
