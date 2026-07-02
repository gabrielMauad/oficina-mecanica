using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.GerarOrdemServico;

public sealed class GerarOrdemServicoHandler : IRequestHandler<GerarOrdemServicoCommand, Result<OrdemServicoResumoDto>>
{
    private readonly IClienteGateway _clienteGateway;
    private readonly IVeiculoGateway _veiculoGateway;
    private readonly IOrdemServicoGateway _ordemServicoGateway;

    public GerarOrdemServicoHandler(
        IClienteGateway clienteGateway,
        IVeiculoGateway veiculoGateway,
        IOrdemServicoGateway ordemServicoGateway
    )
    {
        _clienteGateway = clienteGateway;
        _veiculoGateway = veiculoGateway;
        _ordemServicoGateway = ordemServicoGateway;
    }

    public async Task<Result<OrdemServicoResumoDto>> Handle(GerarOrdemServicoCommand command, CancellationToken ct)
    {
        if (!await _clienteGateway.ExisteEAtivo(command.ClienteId, ct))
            return OrdemServicoErrors.ClienteInexistenteOuInativo;

        if (!await _veiculoGateway.ExisteEPertenceAoCliente(command.VeiculoId, command.ClienteId, ct))
            return OrdemServicoErrors.VeiculoInexistenteOuNaoPertenceAoCliente;

        var result = OrdemServico.Criar(command.ClienteId, command.VeiculoId);
        if (result.IsFailure) return result.Error;

        var os = result.Value;
        await _ordemServicoGateway.Adicionar(os, ct);

        return new OrdemServicoResumoDto(
            os.Id.Value,
            os.ClienteId,
            os.VeiculoId,
            os.Status.ToString(),
            os.DescricaoDiagnostico,
            os.NotificadoEm,
            os.EntregueEm,
            os.CriadoEm,
            os.AtualizadoEm,
            [],
            [],
            []
        );
    }
}
