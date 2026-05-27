using MediatR;
using Microsoft.Extensions.Logging;
using OrdensServico.Application.Ports;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Domain.OrdemServico.Events;
using OrdensServico.Domain.Ports;
using OrdensServico.Domain.Ports.Dtos;
using SharedKernel.Application;
using SharedKernel.Domain;

namespace OrdensServico.Application.DomainEventHandlers;

public sealed class NotificarClienteAoFinalizar : INotificationHandler<OrdemServicoFinalizada>
{
    private readonly IOrdemServicoRepository _ordemServicoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificacaoClientePort _notificacaoClientePort;
    private readonly IClienteInfoPort _clienteInfoPort;
    private readonly IVeiculoInfoPort _veiculoInfoPort;
    private readonly ILogger<NotificarClienteAoFinalizar> _logger;

    public NotificarClienteAoFinalizar(
        IOrdemServicoRepository ordemServicoRepository,
        IUnitOfWork unitOfWork,
        INotificacaoClientePort notificacaoClientePort,
        IClienteInfoPort clienteInfoPort,
        IVeiculoInfoPort veiculoInfoPort,
        ILogger<NotificarClienteAoFinalizar> logger)
    {
        _ordemServicoRepository = ordemServicoRepository;
        _unitOfWork = unitOfWork;
        _notificacaoClientePort = notificacaoClientePort;
        _clienteInfoPort = clienteInfoPort;
        _veiculoInfoPort = veiculoInfoPort;
        _logger = logger;
    }

    public async Task Handle(OrdemServicoFinalizada notification, CancellationToken ct)
    {
        OrdemServico? ordemServico = await _ordemServicoRepository.ObterPorId(notification.OrdemServicoId, ct);
        if (ordemServico is null)
        {
            _logger.LogError(
                "OS {OrdemServicoId} não encontrada ao tentar notificar cliente após OrdemServicoFinalizada.",
                notification.OrdemServicoId);
            return;
        }

        Result<OrdemServico> result = ordemServico.NotificarCliente(DateTime.UtcNow);
        if (result.IsFailure)
        {
            _logger.LogError(
                "Falha ao chamar NotificarCliente na OS {OrdemServicoId}: {Erro}.",
                notification.OrdemServicoId,
                result.Error);
            return;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        ClienteInfo? cliente = await _clienteInfoPort.ObterInfo(ordemServico.ClienteId, ct);
        string? placa = await _veiculoInfoPort.ObterPlaca(ordemServico.VeiculoId, ct);

        if (cliente is null || placa is null)
        {
            _logger.LogWarning(
                "Não foi possível notificar cliente da OS {OrdemServicoId}: cliente={ClienteEncontrado}, placa={PlacaEncontrada}.",
                notification.OrdemServicoId,
                cliente is not null,
                placa is not null);
            return;
        }

        await _notificacaoClientePort.NotificarServicoFinalizado(
            cliente.Nome,
            cliente.Email,
            placa,
            ct
        );
    }
}
