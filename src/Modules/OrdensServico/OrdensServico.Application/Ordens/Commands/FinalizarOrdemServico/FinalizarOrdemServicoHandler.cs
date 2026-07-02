using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.FinalizarOrdemServico;

public sealed class FinalizarOrdemServicoHandler : IRequestHandler<FinalizarOrdemServicoCommand, Result<OrdemServico>>
{
    private readonly IOrdemServicoGateway _ordemServicoGateway;
    public FinalizarOrdemServicoHandler(IOrdemServicoGateway ordemServicoGateway) =>
    _ordemServicoGateway = ordemServicoGateway;

    public async Task<Result<OrdemServico>> Handle(FinalizarOrdemServicoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _ordemServicoGateway.ObterPorId(ordemServicoId, ct);

        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        Result<OrdemServico> result = ordemServico.Finalizar();
        if (result.IsFailure)
            return result.Error;

        OrdemServico os = result.Value;
        await _ordemServicoGateway.Atualizar(os, ct);

        return os;
    }
}
