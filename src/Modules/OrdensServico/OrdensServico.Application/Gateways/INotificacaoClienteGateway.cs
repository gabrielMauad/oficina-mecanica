using OrdensServico.Application.Gateways.Dtos;

namespace OrdensServico.Application.Gateways;

public interface INotificacaoClienteGateway
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
