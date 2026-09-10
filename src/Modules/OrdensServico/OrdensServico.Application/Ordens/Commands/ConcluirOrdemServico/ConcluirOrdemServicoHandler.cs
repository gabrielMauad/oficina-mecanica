using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Application.Metrics;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.ConcluirOrdemServico;

public sealed class ConcluirOrdemServicoHandler : IRequestHandler<ConcluirOrdemServicoCommand, Result<OrdemServico>>
{
    private readonly IOrdemServicoGateway _ordemServicoGateway;
    private readonly OrdensServicoMetrics _metrics;

    public ConcluirOrdemServicoHandler(IOrdemServicoGateway ordemServicoGateway, OrdensServicoMetrics metrics)
    {
        _ordemServicoGateway = ordemServicoGateway;
        _metrics = metrics;
    }

    public async Task<Result<OrdemServico>> Handle(ConcluirOrdemServicoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _ordemServicoGateway.ObterPorId(ordemServicoId, ct);

        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        DateTime inicioEtapa = ordemServico.StatusAlteradoEm;
        Result<OrdemServico> result = ordemServico.Concluir(DateTime.UtcNow);
        if (result.IsFailure)
            return result.Error;

        OrdemServico os = result.Value;
        _metrics.RegistrarDuracaoEtapa("finalizacao", (DateTime.UtcNow - inicioEtapa).TotalSeconds);
        await _ordemServicoGateway.Atualizar(os, ct);

        return os;
    }
}
