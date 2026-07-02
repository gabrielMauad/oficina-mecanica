using OrdensServico.Application.Gateways;
using OrdensServico.Application.Ordens.Commands.IniciarDiagnostico;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Tests.Commands;

public class IniciarDiagnosticoHandlerTests
{
    private readonly Mock<IOrdemServicoGateway> _repoMock = new();
    private readonly IniciarDiagnosticoHandler _handler;

    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();

    public IniciarDiagnosticoHandlerTests()
    {
        _handler = new(_repoMock.Object);
    }

    [Fact(DisplayName = "Cenário feliz: OS Recebida → status muda para EmDiagnostico")]
    public async Task Handle_OsRecebida_TransitaParaEmDiagnostico()
    {
        var os = OrdensServico.Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId).Value;
        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        var command = new IniciarDiagnosticoCommand(os.Id.Value);
        Result<OrdemServicoResumoDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EmDiagnostico", result.Value.Status);
        _repoMock.Verify(x => x.Atualizar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: OS não encontrada → NaoEncontrada")]
    public async Task Handle_OsNaoEncontrada_RetornaErroNaoEncontrada()
    {
        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdensServico.Domain.OrdemServico.OrdemServico?)null);

        var command = new IniciarDiagnosticoCommand(Guid.NewGuid());
        Result<OrdemServicoResumoDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.NaoEncontrada", result.Error.Code);
        _repoMock.Verify(x => x.Atualizar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
