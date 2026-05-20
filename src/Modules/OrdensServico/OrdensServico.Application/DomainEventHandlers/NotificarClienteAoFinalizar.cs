using MediatR;
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

    public NotificarClienteAoFinalizar(
        IOrdemServicoRepository ordemServicoRepository,
        IUnitOfWork unitOfWork,
        INotificacaoClientePort notificacaoClientePort,
        IClienteInfoPort clienteInfoPort,
        IVeiculoInfoPort veiculoInfoPort
    )
    {
        _ordemServicoRepository = ordemServicoRepository;
        _unitOfWork = unitOfWork;
        _notificacaoClientePort = notificacaoClientePort;
        _clienteInfoPort = clienteInfoPort;
        _veiculoInfoPort = veiculoInfoPort;
    }

    public async Task Handle(OrdemServicoFinalizada notification, CancellationToken ct)
    {
        OrdemServico? ordemServico = await _ordemServicoRepository.ObterPorId(notification.OrdemServicoId, ct);
        if (ordemServico is null)
            return;

        Result<OrdemServico> result = ordemServico.NotificarCliente(DateTime.UtcNow);
        if (result.IsFailure)
            return;

        await _unitOfWork.SaveChangesAsync(ct);

        Task<ClienteInfo?> clienteTask = _clienteInfoPort.ObterInfo(ordemServico.ClienteId, ct);
        Task<string?> placaTask = _veiculoInfoPort.ObterPlaca(ordemServico.VeiculoId, ct);
        await Task.WhenAll(clienteTask, placaTask);

        ClienteInfo? cliente = await clienteTask;
        string? placa = await placaTask;

        if (cliente is null || placa is null)
            return;

        await _notificacaoClientePort.NotificarServicoFinalizado(
            cliente.Nome,
            cliente.Email,
            placa,
            ct
        );
    }
}
