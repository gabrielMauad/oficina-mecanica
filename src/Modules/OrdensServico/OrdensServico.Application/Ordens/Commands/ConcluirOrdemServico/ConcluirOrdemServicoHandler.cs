using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.ConcluirOrdemServico;

public sealed class ConcluirOrdemServicoHandler : IRequestHandler<ConcluirOrdemServicoCommand, Result<OrdemServico>>
{
    private readonly IOrdemServicoGateway _ordemServicoGateway;

    public ConcluirOrdemServicoHandler(IOrdemServicoGateway ordemServicoGateway) =>
    _ordemServicoGateway = ordemServicoGateway;

    public async Task<Result<OrdemServico>> Handle(ConcluirOrdemServicoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _ordemServicoGateway.ObterPorId(ordemServicoId, ct);

        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        Result<OrdemServico> result = ordemServico.Concluir(DateTime.UtcNow);
        if (result.IsFailure)
            return result.Error;

        OrdemServico os = result.Value;
        await _ordemServicoGateway.Atualizar(os, ct);

        return os;
    }
}
