using MediatR;
using Microsoft.Extensions.Logging;
using OrdensServico.Application.Gateways;
using OrdensServico.Application.Gateways.Dtos;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Domain.OrdemServico.Events;
using SharedKernel.Application;
using SharedKernel.Domain;

namespace OrdensServico.Application.DomainEventHandlers;

public sealed class NotificarClienteAoFinalizar : INotificationHandler<OrdemServicoFinalizada>
{
    private readonly IOrdemServicoGateway _ordemServicoGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificacaoClienteGateway _notificacaoClienteGateway;
    private readonly IClienteGateway _clienteGateway;
    private readonly IVeiculoGateway _veiculoGateway;
    private readonly ILogger<NotificarClienteAoFinalizar> _logger;

    public NotificarClienteAoFinalizar(
        IOrdemServicoGateway ordemServicoGateway,
        IUnitOfWork unitOfWork,
        INotificacaoClienteGateway notificacaoClienteGateway,
        IClienteGateway clienteGateway,
        IVeiculoGateway veiculoGateway,
        ILogger<NotificarClienteAoFinalizar> logger)
    {
        _ordemServicoGateway = ordemServicoGateway;
        _unitOfWork = unitOfWork;
        _notificacaoClienteGateway = notificacaoClienteGateway;
        _clienteGateway = clienteGateway;
        _veiculoGateway = veiculoGateway;
        _logger = logger;
    }

    public async Task Handle(OrdemServicoFinalizada notification, CancellationToken ct)
    {
        OrdemServico? ordemServico = await _ordemServicoGateway.ObterPorId(notification.OrdemServicoId, ct);
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

        ClienteInfo? cliente = await _clienteGateway.ObterInfo(ordemServico.ClienteId, ct);
        string? placa = await _veiculoGateway.ObterPlaca(ordemServico.VeiculoId, ct);

        if (cliente is null || placa is null)
        {
            _logger.LogWarning(
                "Não foi possível notificar cliente da OS {OrdemServicoId}: cliente={ClienteEncontrado}, placa={PlacaEncontrada}.",
                notification.OrdemServicoId,
                cliente is not null,
                placa is not null);
            return;
        }

        await _notificacaoClienteGateway.NotificarServicoFinalizado(
            cliente.Nome,
            cliente.Email,
            placa,
            ct
        );
    }
}
