using OrdensServico.Application.Gateways;
using OrdensServico.Application.Ordens.Commands.RejeitarOrcamento;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Tests.Commands;

public class RejeitarOrcamentoHandlerTests
{
    private readonly Mock<IOrdemServicoGateway> _repoMock = new();
    private readonly RejeitarOrcamentoHandler _handler;

    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ServicoId = Guid.NewGuid();
    private static readonly Guid PecaId = Guid.NewGuid();

    public RejeitarOrcamentoHandlerTests()
    {
        _handler = new(_repoMock.Object);
    }

    private static OrdensServico.Domain.OrdemServico.OrdemServico CriarOsAguardandoAprovacao()
    {
        var os = OrdensServico.Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId).Value;
        os.IniciarDiagnostico();
        os.RegistrarDiagnostico("desc", [new ItemServicoInput(ServicoId, 1, 100m)], [new ItemPecaInput(PecaId, 1, 50m)]);
        os.EnviarOrcamento(DateTime.UtcNow);
        return os;
    }

    [Fact(DisplayName = "Cenário feliz: OS aguardando aprovação com orçamento enviado → orçamento rejeitado")]
    public async Task Handle_OsAguardandoAprovacaoComOrcamentoEnviado_RejeicaoSucede()
    {
        var os = CriarOsAguardandoAprovacao();
        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        var command = new RejeitarOrcamentoCommand(os.Id.Value);
        Result<OrdemServicoResumoDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Rejeitado", result.Value.Orcamentos[0].Status);
        _repoMock.Verify(x => x.Atualizar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: OS não encontrada → NaoEncontrada")]
    public async Task Handle_OsNaoEncontrada_RetornaErroNaoEncontrada()
    {
        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdensServico.Domain.OrdemServico.OrdemServico?)null);

        var command = new RejeitarOrcamentoCommand(Guid.NewGuid());
        Result<OrdemServicoResumoDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.NaoEncontrada", result.Error.Code);
        _repoMock.Verify(x => x.Atualizar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
