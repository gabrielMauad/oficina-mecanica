using MediatR;
using OrdensServico.Application.Ports;
using OrdensServico.Application.Ports.Dtos;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Domain.OrdemServico.Events;
using OrdensServico.Domain.Ports;
using OrdensServico.Domain.Ports.Dtos;
using SharedKernel.Application;
using SharedKernel.Domain;

namespace OrdensServico.Application.DomainEventHandlers;

public sealed class EnviarOrcamentoAoCliente : INotificationHandler<DiagnosticoConcluido>
{
    private readonly IOrdemServicoRepository _ordemServicoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificacaoClientePort _notificacaoClientePort;
    private readonly IClienteInfoPort _clienteInfoPort;
    private readonly IVeiculoInfoPort _veiculoInfoPort;
    private readonly IServicoInfoPort _servicoInfoPort;
    private readonly IPecaInsumoInfoPort _pecaInsumoInfoPort;

    public EnviarOrcamentoAoCliente(
        IOrdemServicoRepository ordemServicoRepository,
        IUnitOfWork unitOfWork,
        INotificacaoClientePort notificacaoClientePort,
        IClienteInfoPort clienteInfoPort,
        IVeiculoInfoPort veiculoInfoPort,
        IServicoInfoPort servicoInfoPort,
        IPecaInsumoInfoPort pecaInsumoInfoPort)
    {
        _ordemServicoRepository = ordemServicoRepository;
        _unitOfWork = unitOfWork;
        _notificacaoClientePort = notificacaoClientePort;
        _clienteInfoPort = clienteInfoPort;
        _veiculoInfoPort = veiculoInfoPort;
        _servicoInfoPort = servicoInfoPort;
        _pecaInsumoInfoPort = pecaInsumoInfoPort;
    }

    public async Task Handle(DiagnosticoConcluido notification, CancellationToken ct)
    {
        OrdemServico? ordemServico = await _ordemServicoRepository.ObterPorId(notification.OrdemServicoId, ct);
        if (ordemServico is null)
            return;

        Result<OrdemServico> result = ordemServico.EnviarOrcamento(DateTime.UtcNow);
        if (result.IsFailure)
            return;

        await _unitOfWork.SaveChangesAsync(ct);

        ClienteInfo? cliente = await _clienteInfoPort.ObterInfo(ordemServico.ClienteId, ct);
        string? placa = await _veiculoInfoPort.ObterPlaca(ordemServico.VeiculoId, ct);

        if (cliente is null || placa is null)
            return;

        List<ServicoEmailItem> servicos = [];
        foreach (ItemServico item in ordemServico.ItensServico)
        {
            string? nome = await _servicoInfoPort.ObterNome(item.ServicoId, ct);
            if (nome is not null)
                servicos.Add(new ServicoEmailItem(nome, item.Quantidade, item.PrecoUnitarioSnapshot));
        }

        List<PecaEmailItem> pecas = [];
        foreach (ItemPeca item in ordemServico.ItensPeca)
        {
            PecaInsumoInfo? info = await _pecaInsumoInfoPort.Obter(item.PecaInsumoId, ct);
            if (info is not null)
                pecas.Add(new PecaEmailItem(info.Nome, item.Quantidade, info.UnidadeMedida, item.PrecoUnitarioSnapshot));
        }

        await _notificacaoClientePort.NotificarOrcamentoPronto(
            cliente.Nome,
            cliente.Email,
            placa,
            servicos,
            pecas,
            notification.ValorTotal,
            ct);
    }
}
