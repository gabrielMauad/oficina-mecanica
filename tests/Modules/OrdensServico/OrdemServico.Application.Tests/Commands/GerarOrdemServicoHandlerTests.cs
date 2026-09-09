using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using OrdensServico.Application.Gateways;
using OrdensServico.Application.Metrics;
using OrdensServico.Application.Ordens.Commands.GerarOrdemServico;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Tests.Commands;

public class GerarOrdemServicoHandlerTests
{
    private readonly Mock<IClienteGateway> _clienteMock = new();
    private readonly Mock<IVeiculoGateway> _veiculoMock = new();
    private readonly Mock<IOrdemServicoGateway> _repoMock = new();
    private readonly OrdensServicoMetrics _metrics = new();
    private readonly GerarOrdemServicoHandler _handler;

    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();

    public GerarOrdemServicoHandlerTests()
    {
        _handler = new(_clienteMock.Object, _veiculoMock.Object, _repoMock.Object, _metrics);
    }

    [Fact(DisplayName = "Cenário feliz: cliente ativo e veículo pertence ao cliente → cria OS com status Recebida")]
    public async Task Handle_ClienteAtivoEVeiculoValido_CriaOrdemServico()
    {
        using var collector = new MetricCollector<long>(_metrics.Meter, OrdensServicoMetrics.OrdensAbertasInstrumentName);
        _clienteMock.Setup(x => x.ExisteEAtivo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _veiculoMock.Setup(x => x.ExisteEPertenceAoCliente(VeiculoId, ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new GerarOrdemServicoCommand(ClienteId, VeiculoId);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClienteId, result.Value.ClienteId);
        Assert.Equal(VeiculoId, result.Value.VeiculoId);
        Assert.Equal("Recebida", result.Value.Status.ToString());
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Once);

        var medicao = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(1, medicao.Value);
        Assert.Equal("simples", medicao.Tags["tipo"]);
    }

    [Fact(DisplayName = "Erro: cliente inexistente ou inativo → ClienteInexistenteOuInativo")]
    public async Task Handle_ClienteInexistente_RetornaErroClienteInexistenteOuInativo()
    {
        using var collector = new MetricCollector<long>(_metrics.Meter, OrdensServicoMetrics.OrdensAbertasInstrumentName);
        _clienteMock.Setup(x => x.ExisteEAtivo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new GerarOrdemServicoCommand(ClienteId, VeiculoId);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.ClienteInexistenteOuInativo", result.Error.Code);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(collector.GetMeasurementSnapshot());
    }

    [Fact(DisplayName = "Erro: veículo não pertence ao cliente → VeiculoInexistenteOuNaoPertenceAoCliente")]
    public async Task Handle_VeiculoNaoPertenceAoCliente_RetornaErroVeiculoInexistente()
    {
        _clienteMock.Setup(x => x.ExisteEAtivo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _veiculoMock.Setup(x => x.ExisteEPertenceAoCliente(VeiculoId, ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new GerarOrdemServicoCommand(ClienteId, VeiculoId);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.VeiculoInexistenteOuNaoPertenceAoCliente", result.Error.Code);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
