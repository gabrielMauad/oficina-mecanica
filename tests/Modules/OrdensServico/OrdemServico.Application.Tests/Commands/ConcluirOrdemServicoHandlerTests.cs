using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using OrdensServico.Application.Gateways;
using OrdensServico.Application.Metrics;
using OrdensServico.Application.Ordens.Commands.ConcluirOrdemServico;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Tests.Commands;

public class ConcluirOrdemServicoHandlerTests
{
    private readonly Mock<IOrdemServicoGateway> _repoMock = new();
    private readonly OrdensServicoMetrics _metrics = new();
    private readonly ConcluirOrdemServicoHandler _handler;

    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ServicoId = Guid.NewGuid();
    private static readonly Guid PecaId = Guid.NewGuid();

    public ConcluirOrdemServicoHandlerTests()
    {
        _handler = new(_repoMock.Object, _metrics);
    }

    private static OrdensServico.Domain.OrdemServico.OrdemServico CriarOsFinalizadaENotificada()
    {
        var os = OrdensServico.Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId).Value;
        os.IniciarDiagnostico();
        os.RegistrarDiagnostico("desc", [new ItemServicoInput(ServicoId, 1, 100m)], [new ItemPecaInput(PecaId, 1, 50m)]);
        os.EnviarOrcamento(DateTime.UtcNow);
        os.AprovarOrcamento();
        os.Executar();
        os.Finalizar();
        os.NotificarCliente(DateTime.UtcNow);
        return os;
    }

    private static OrdensServico.Domain.OrdemServico.OrdemServico CriarOsFinalizada()
    {
        var os = OrdensServico.Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId).Value;
        os.IniciarDiagnostico();
        os.RegistrarDiagnostico("desc", [new ItemServicoInput(ServicoId, 1, 100m)], [new ItemPecaInput(PecaId, 1, 50m)]);
        os.EnviarOrcamento(DateTime.UtcNow);
        os.AprovarOrcamento();
        os.Executar();
        os.Finalizar();
        return os;
    }

    [Fact(DisplayName = "Cenário feliz: OS finalizada e notificada → status Entregue")]
    public async Task Handle_OsFinalizadaENotificada_TransitaParaEntregue()
    {
        using var collector = new MetricCollector<double>(_metrics.Meter, OrdensServicoMetrics.EtapaDuracaoInstrumentName);
        var os = CriarOsFinalizadaENotificada();
        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        var command = new ConcluirOrdemServicoCommand(os.Id.Value);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Entregue", result.Value.Status.ToString());
        Assert.NotNull(result.Value.EntregueEm);
        _repoMock.Verify(x => x.Atualizar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Once);

        var medicao = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.True(medicao.Value >= 0);
        Assert.Equal("finalizacao", medicao.Tags["etapa"]);
    }

    [Fact(DisplayName = "NotificarCliente entre Finalizar e Concluir: duração da etapa finalização mede desde Finalizar, não desde a notificação")]
    public async Task Handle_ComNotificacaoEntreFinalizarEConcluir_MedeDuracaoDesdeFinalizarNaoDesdeNotificacao()
    {
        using var collector = new MetricCollector<double>(_metrics.Meter, OrdensServicoMetrics.EtapaDuracaoInstrumentName);
        var os = CriarOsFinalizada();

        // Esta é a sequência que motivou a correção: NotificarCliente roda entre Finalizar e
        // Concluir e sobrescreve AtualizadoEm, mas NÃO sobrescreve StatusAlteradoEm — que continua
        // marcando o instante de Finalizar. Um atraso real aqui simula o tempo entre a OS ficar
        // pronta e o cliente ser avisado.
        await Task.Delay(150);
        os.NotificarCliente(DateTime.UtcNow);

        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        var command = new ConcluirOrdemServicoCommand(os.Id.Value);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var medicao = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal("finalizacao", medicao.Tags["etapa"]);
        // Se a métrica ainda usasse AtualizadoEm (sobrescrito por NotificarCliente), a duração
        // medida seria próxima de zero. Medindo a partir de StatusAlteradoEm (fixado em Finalizar),
        // ela precisa refletir ao menos o atraso inserido antes da notificação.
        Assert.True(medicao.Value >= 0.1,
            $"Duração medida ({medicao.Value}s) deveria refletir o intervalo desde Finalizar (>= 0.1s), não desde NotificarCliente.");
    }

    [Fact(DisplayName = "Erro: OS não encontrada → NaoEncontrada")]
    public async Task Handle_OsNaoEncontrada_RetornaErroNaoEncontrada()
    {
        using var collector = new MetricCollector<double>(_metrics.Meter, OrdensServicoMetrics.EtapaDuracaoInstrumentName);
        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdensServico.Domain.OrdemServico.OrdemServico?)null);

        var command = new ConcluirOrdemServicoCommand(Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.NaoEncontrada", result.Error.Code);
        _repoMock.Verify(x => x.Atualizar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(collector.GetMeasurementSnapshot());
    }
}
