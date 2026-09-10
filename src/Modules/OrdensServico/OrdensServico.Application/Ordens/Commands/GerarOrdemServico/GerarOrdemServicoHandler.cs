using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Application.Metrics;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.GerarOrdemServico;

public sealed class GerarOrdemServicoHandler : IRequestHandler<GerarOrdemServicoCommand, Result<OrdemServico>>
{
    private readonly IClienteGateway _clienteGateway;
    private readonly IVeiculoGateway _veiculoGateway;
    private readonly IOrdemServicoGateway _ordemServicoGateway;
    private readonly OrdensServicoMetrics _metrics;

    public GerarOrdemServicoHandler(
        IClienteGateway clienteGateway,
        IVeiculoGateway veiculoGateway,
        IOrdemServicoGateway ordemServicoGateway,
        OrdensServicoMetrics metrics
    )
    {
        _clienteGateway = clienteGateway;
        _veiculoGateway = veiculoGateway;
        _ordemServicoGateway = ordemServicoGateway;
        _metrics = metrics;
    }

    public async Task<Result<OrdemServico>> Handle(GerarOrdemServicoCommand command, CancellationToken ct)
    {
        if (!await _clienteGateway.ExisteEAtivo(command.ClienteId, ct))
            return OrdemServicoErrors.ClienteInexistenteOuInativo;

        if (!await _veiculoGateway.ExisteEPertenceAoCliente(command.VeiculoId, command.ClienteId, ct))
            return OrdemServicoErrors.VeiculoInexistenteOuNaoPertenceAoCliente;

        var result = OrdemServico.Criar(command.ClienteId, command.VeiculoId);
        if (result.IsFailure) return result.Error;

        var os = result.Value;
        await _ordemServicoGateway.Adicionar(os, ct);
        _metrics.RegistrarOrdemAberta("simples");

        return os;
    }
}
