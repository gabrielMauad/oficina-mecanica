using OrdensServico.Application.Gateways;
using OrdensServico.Application.Gateways.Dtos;
using OrdensServico.Application.Ordens.Commands.AbrirOrdemServicoCompleta;
using OrdensServico.Application.Ordens.Commands.RegistrarDiagnostico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Tests.Commands;

public class AbrirOrdemServicoCompletaHandlerTests
{
    private readonly Mock<IClienteGateway> _clienteMock = new();
    private readonly Mock<IVeiculoGateway> _veiculoMock = new();
    private readonly Mock<IServicoGateway> _servicoMock = new();
    private readonly Mock<IPecaDisponibilidadeGateway> _pecaMock = new();
    private readonly Mock<IOrdemServicoGateway> _repoMock = new();
    private readonly AbrirOrdemServicoCompletaHandler _handler;

    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ServicoId = Guid.NewGuid();
    private static readonly Guid PecaId = Guid.NewGuid();

    public AbrirOrdemServicoCompletaHandlerTests()
    {
        _handler = new(_clienteMock.Object, _veiculoMock.Object, _servicoMock.Object, _pecaMock.Object, _repoMock.Object);
    }

    private AbrirOrdemServicoCompletaCommand CriarCommandValido() => new(
        ClienteId,
        VeiculoId,
        [new ServicoItem(ServicoId, 1)],
        [new PecaItem(PecaId, 2)]);

    private void ConfigurarClienteEVeiculoValidos()
    {
        _clienteMock.Setup(x => x.ExisteEAtivo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _veiculoMock.Setup(x => x.ExisteEPertenceAoCliente(VeiculoId, ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact(DisplayName = "Cenário feliz: cliente/veículo válidos, serviço e peça disponíveis → cria OS AguardandoAprovacao")]
    public async Task Handle_DadosValidos_CriaOrdemServicoAguardandoAprovacao()
    {
        ConfigurarClienteEVeiculoValidos();
        _servicoMock.Setup(x => x.ObterPreco(ServicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100m);
        _pecaMock.Setup(x => x.Verificar(PecaId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PecaDisponibilidade(true, 50m));

        var result = await _handler.Handle(CriarCommandValido(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("AguardandoAprovacao", result.Value.Status.ToString());
        Assert.Null(result.Value.DescricaoDiagnostico);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: cliente inexistente ou inativo → ClienteInexistenteOuInativo")]
    public async Task Handle_ClienteInexistente_RetornaErroClienteInexistenteOuInativo()
    {
        _clienteMock.Setup(x => x.ExisteEAtivo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(CriarCommandValido(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.ClienteInexistenteOuInativo", result.Error.Code);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Erro: veículo não pertence ao cliente → VeiculoInexistenteOuNaoPertenceAoCliente")]
    public async Task Handle_VeiculoNaoPertenceAoCliente_RetornaErroVeiculoInexistente()
    {
        _clienteMock.Setup(x => x.ExisteEAtivo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _veiculoMock.Setup(x => x.ExisteEPertenceAoCliente(VeiculoId, ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(CriarCommandValido(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.VeiculoInexistenteOuNaoPertenceAoCliente", result.Error.Code);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Erro: serviço inexistente → ServicoNaoEncontrado")]
    public async Task Handle_ServicoInexistente_RetornaErroServicoNaoEncontrado()
    {
        ConfigurarClienteEVeiculoValidos();
        _servicoMock.Setup(x => x.ObterPreco(ServicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        var result = await _handler.Handle(CriarCommandValido(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.ServicoNaoEncontrado", result.Error.Code);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Erro: peça indisponível → PecaIndisponivel")]
    public async Task Handle_PecaIndisponivel_RetornaErroPecaIndisponivel()
    {
        ConfigurarClienteEVeiculoValidos();
        _servicoMock.Setup(x => x.ObterPreco(ServicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100m);
        _pecaMock.Setup(x => x.Verificar(PecaId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PecaDisponibilidade(false, 50m));

        var result = await _handler.Handle(CriarCommandValido(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.PecaIndisponivel", result.Error.Code);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Erro: peça inexistente → PecaNaoEncontrada")]
    public async Task Handle_PecaInexistente_RetornaErroPecaNaoEncontrada()
    {
        ConfigurarClienteEVeiculoValidos();
        _servicoMock.Setup(x => x.ObterPreco(ServicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100m);
        _pecaMock.Setup(x => x.Verificar(PecaId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PecaDisponibilidade?)null);

        var result = await _handler.Handle(CriarCommandValido(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.PecaNaoEncontrada", result.Error.Code);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
