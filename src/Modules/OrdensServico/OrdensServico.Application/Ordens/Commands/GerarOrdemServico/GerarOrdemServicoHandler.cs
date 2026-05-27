using MediatR;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Domain.Ports;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.GerarOrdemServico;

public sealed class GerarOrdemServicoHandler : IRequestHandler<GerarOrdemServicoCommand, Result<OrdemServicoResumoDto>>
{
    private readonly IClienteInfoPort _clienteInfoPort;
    private readonly IVeiculoInfoPort _veiculoInfoPort;
    private readonly IOrdemServicoRepository _ordemServicoRepository;

    public GerarOrdemServicoHandler(
        IClienteInfoPort clienteInfoPort,
        IVeiculoInfoPort veiculoInfoPort,
        IOrdemServicoRepository ordemServicoRepository
    )
    {
        _clienteInfoPort = clienteInfoPort;
        _veiculoInfoPort = veiculoInfoPort;
        _ordemServicoRepository = ordemServicoRepository;
    }

    public async Task<Result<OrdemServicoResumoDto>> Handle(GerarOrdemServicoCommand command, CancellationToken ct)
    {
        if (!await _clienteInfoPort.ExisteEAtivo(command.ClienteId, ct))
            return OrdemServicoErrors.ClienteInexistenteOuInativo;

        if (!await _veiculoInfoPort.ExisteEPertenceAoCliente(command.VeiculoId, command.ClienteId, ct))
            return OrdemServicoErrors.VeiculoInexistenteOuNaoPertenceAoCliente;

        var result = OrdemServico.Criar(command.ClienteId, command.VeiculoId);
        if (result.IsFailure) return result.Error;

        var os = result.Value;
        await _ordemServicoRepository.Adicionar(os, ct);

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
