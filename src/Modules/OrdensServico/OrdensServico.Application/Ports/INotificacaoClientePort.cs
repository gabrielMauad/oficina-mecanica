using OrdensServico.Application.Ports.Dtos;

namespace OrdensServico.Application.Ports;

public interface INotificacaoClientePort
{
    Task NotificarOrcamentoPronto(
        string nomeCliente,
        string emailCliente,
        string placaVeiculo,
        IReadOnlyList<ServicoEmailItem> servicos,
        IReadOnlyList<PecaEmailItem> pecas,
        decimal valorTotal,
        CancellationToken ct);

    Task NotificarServicoFinalizado(string nomeCliente, string emailCliente, string placaVeiculo, CancellationToken ct);
}
