using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.IniciarDiagnostico;

public sealed class IniciarDiagnosticoHandler : IRequestHandler<IniciarDiagnosticoCommand, Result<OrdemServico>>
{
    private readonly IOrdemServicoGateway _gateway;

    public IniciarDiagnosticoHandler(
        IOrdemServicoGateway gateway
    ) => _gateway = gateway;

    public async Task<Result<OrdemServico>> Handle(IniciarDiagnosticoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _gateway.ObterPorId(ordemServicoId, ct);
        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        Result<OrdemServico> resultado = ordemServico.IniciarDiagnostico();
        if (resultado.IsFailure)
            return resultado.Error;

        OrdemServico os = resultado.Value;

        await _gateway.Atualizar(os, ct);

        return os;
    }
}
