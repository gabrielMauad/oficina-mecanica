using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Application.Metrics;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.FinalizarOrdemServico;

public sealed class FinalizarOrdemServicoHandler : IRequestHandler<FinalizarOrdemServicoCommand, Result<OrdemServico>>
{
    private readonly IOrdemServicoGateway _ordemServicoGateway;
    private readonly OrdensServicoMetrics _metrics;

    public FinalizarOrdemServicoHandler(IOrdemServicoGateway ordemServicoGateway, OrdensServicoMetrics metrics)
    {
        _ordemServicoGateway = ordemServicoGateway;
        _metrics = metrics;
    }

    public async Task<Result<OrdemServico>> Handle(FinalizarOrdemServicoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _ordemServicoGateway.ObterPorId(ordemServicoId, ct);

        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        DateTime inicioEtapa = ordemServico.AtualizadoEm;
        Result<OrdemServico> result = ordemServico.Finalizar();
        if (result.IsFailure)
            return result.Error;

        OrdemServico os = result.Value;
        _metrics.RegistrarDuracaoEtapa("execucao", (DateTime.UtcNow - inicioEtapa).TotalSeconds);
        await _ordemServicoGateway.Atualizar(os, ct);

        return os;
    }
}
