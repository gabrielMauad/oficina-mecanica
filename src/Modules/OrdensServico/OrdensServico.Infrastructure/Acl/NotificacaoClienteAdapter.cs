using Microsoft.Extensions.Logging;
using OrdensServico.Application.Ports;
using OrdensServico.Application.Ports.Dtos;
using System.Text;

namespace OrdensServico.Infrastructure.Acl;

internal sealed class NotificacaoClienteAdapter : INotificacaoClientePort
{
    private readonly ILogger<NotificacaoClienteAdapter> _logger;

    public NotificacaoClienteAdapter(ILogger<NotificacaoClienteAdapter> logger)
    {
        _logger = logger;
    }

    public async Task NotificarOrcamentoPronto(
        string nomeCliente,
        string emailCliente,
        string placaVeiculo,
        IReadOnlyList<ServicoEmailItem> servicos,
        IReadOnlyList<PecaEmailItem> pecas,
        decimal valorTotal,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Prezado {nomeCliente}, o orcamento do veiculo de placa {placaVeiculo} esta pronto.");
        sb.AppendLine();
        sb.AppendLine("Servicos:");
        foreach (var servico in servicos)
            sb.AppendLine($"- {servico.Nome}: {servico.PrecoUnitario:C}");
        sb.AppendLine();
        sb.AppendLine("Pecas e insumos:");
        foreach (var peca in pecas)
            sb.AppendLine($"- {peca.Nome}: {peca.Quantidade}{peca.UnidadeMedida} x {peca.PrecoUnitario:C}");
        sb.AppendLine();
        sb.AppendLine($"Valor total: {valorTotal:C}");
        sb.AppendLine();
        sb.AppendLine("Pedimos que responda esse email aprovando ou nao a execucao da ordem de servico");

        _logger.LogInformation("[EMAIL SIMULADO] Para: {Email}\n{Corpo}", emailCliente, sb.ToString());

        await Task.CompletedTask;
    }

    public async Task NotificarServicoFinalizado(string nomeCliente, string emailCliente, string placaVeiculo, CancellationToken ct)
    {
        var corpo = $"""
            Prezado {nomeCliente},

            Informamos que o servico do veiculo de placa {placaVeiculo} foi concluido com sucesso.

            O veiculo esta pronto para retirada. Por favor, dirija-se a nossa oficina em horario comercial.

            Agradecemos a preferencia e esperamos te ver novamente!
            """;

        _logger.LogInformation("[EMAIL SIMULADO] Para: {Email}\n{Corpo}", emailCliente, corpo);

        await Task.CompletedTask;
    }
}
