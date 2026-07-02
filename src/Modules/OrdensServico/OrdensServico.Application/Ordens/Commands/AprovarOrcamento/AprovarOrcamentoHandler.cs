using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.AprovarOrcamento;

public sealed class AprovarOrcamentoHandler : IRequestHandler<AprovarOrcamentoCommand, Result<OrdemServico>>
{
    private readonly IOrdemServicoGateway _ordemServicoGateway;

    public AprovarOrcamentoHandler(IOrdemServicoGateway ordemServicoGateway) =>
        _ordemServicoGateway = ordemServicoGateway;

    public async Task<Result<OrdemServico>> Handle(AprovarOrcamentoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _ordemServicoGateway.ObterPorId(ordemServicoId, ct);

        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        Result<OrdemServico> result = ordemServico.AprovarOrcamento();
        if (result.IsFailure)
            return result.Error;

        OrdemServico os = result.Value;
        await _ordemServicoGateway.Atualizar(os, ct);

        return os;
    }
}
