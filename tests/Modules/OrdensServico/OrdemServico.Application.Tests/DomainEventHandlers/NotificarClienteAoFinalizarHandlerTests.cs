using Microsoft.Extensions.Logging.Abstractions;
using OrdensServico.Application.DomainEventHandlers;
using OrdensServico.Application.Ports;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Domain.OrdemServico.Events;
using OrdensServico.Domain.Ports;
using OrdensServico.Domain.Ports.Dtos;
using SharedKernel.Application;

namespace OrdensServico.Application.Tests.DomainEventHandlers;

public class NotificarClienteAoFinalizarHandlerTests
{
    private readonly Mock<IOrdemServicoRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<INotificacaoClientePort> _notificacaoMock = new();
    private readonly Mock<IClienteInfoPort> _clienteMock = new();
    private readonly Mock<IVeiculoInfoPort> _veiculoMock = new();
    private readonly NotificarClienteAoFinalizar _handler;

    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ServicoId = Guid.NewGuid();
    private static readonly Guid PecaId = Guid.NewGuid();

    public NotificarClienteAoFinalizarHandlerTests()
    {
        _handler = new(
            _repoMock.Object,
            _uowMock.Object,
            _notificacaoMock.Object,
            _clienteMock.Object,
            _veiculoMock.Object,
            NullLogger<NotificarClienteAoFinalizar>.Instance);
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

    private static OrdemServicoFinalizada CriarNotificacao(OrdensServico.Domain.OrdemServico.OrdemServico os) =>
        os.DomainEvents.OfType<OrdemServicoFinalizada>().Single();

    [Fact(DisplayName = "Cenário feliz: OS encontrada → NotificarCliente, Save e NotificarServicoFinalizado chamados")]
    public async Task Handle_OsEncontrada_NotificaClienteEFinaliza()
    {
        var os = CriarOsFinalizada();
        var notification = CriarNotificacao(os);

        _repoMock.Setup(x => x.ObterPorId(notification.OrdemServicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _clienteMock.Setup(x => x.ObterInfo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteInfo("Maria Santos", "maria@email.com"));
        _veiculoMock.Setup(x => x.ObterPlaca(VeiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("XYZ9W87");

        await _handler.Handle(notification, CancellationToken.None);

        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificacaoMock.Verify(x => x.NotificarServicoFinalizado(
            "Maria Santos",
            "maria@email.com",
            "XYZ9W87",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "OS não encontrada: retorna silenciosamente sem notificar")]
    public async Task Handle_OsNaoEncontrada_RetornaSemNotificar()
    {
        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdensServico.Domain.OrdemServico.OrdemServico?)null);

        var os = CriarOsFinalizada();
        var notification = CriarNotificacao(os);

        await _handler.Handle(notification, CancellationToken.None);

        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notificacaoMock.Verify(x => x.NotificarServicoFinalizado(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Cliente ou placa nulos: salva mas não notifica")]
    public async Task Handle_ClienteOuPlacaNull_SalvaMasNaoNotifica()
    {
        var os = CriarOsFinalizada();
        var notification = CriarNotificacao(os);

        _repoMock.Setup(x => x.ObterPorId(notification.OrdemServicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _clienteMock.Setup(x => x.ObterInfo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteInfo("Maria Santos", "maria@email.com"));
        _veiculoMock.Setup(x => x.ObterPlaca(VeiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null); // placa não encontrada

        await _handler.Handle(notification, CancellationToken.None);

        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificacaoMock.Verify(x => x.NotificarServicoFinalizado(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
