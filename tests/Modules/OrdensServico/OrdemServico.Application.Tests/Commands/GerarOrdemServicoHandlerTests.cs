using OrdensServico.Application.Gateways;
using OrdensServico.Application.Ordens.Commands.GerarOrdemServico;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Tests.Commands;

public class GerarOrdemServicoHandlerTests
{
    private readonly Mock<IClienteGateway> _clienteMock = new();
    private readonly Mock<IVeiculoGateway> _veiculoMock = new();
    private readonly Mock<IOrdemServicoGateway> _repoMock = new();
    private readonly GerarOrdemServicoHandler _handler;

    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();

    public GerarOrdemServicoHandlerTests()
    {
        _handler = new(_clienteMock.Object, _veiculoMock.Object, _repoMock.Object);
    }

    [Fact(DisplayName = "Cenário feliz: cliente ativo e veículo pertence ao cliente → cria OS com status Recebida")]
    public async Task Handle_ClienteAtivoEVeiculoValido_CriaOrdemServico()
    {
        _clienteMock.Setup(x => x.ExisteEAtivo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _veiculoMock.Setup(x => x.ExisteEPertenceAoCliente(VeiculoId, ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new GerarOrdemServicoCommand(ClienteId, VeiculoId);
        Result<OrdemServicoResumoDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClienteId, result.Value.ClienteId);
        Assert.Equal(VeiculoId, result.Value.VeiculoId);
        Assert.Equal("Recebida", result.Value.Status);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: cliente inexistente ou inativo → ClienteInexistenteOuInativo")]
    public async Task Handle_ClienteInexistente_RetornaErroClienteInexistenteOuInativo()
    {
        _clienteMock.Setup(x => x.ExisteEAtivo(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new GerarOrdemServicoCommand(ClienteId, VeiculoId);
        Result<OrdemServicoResumoDto> result = await _handler.Handle(command, CancellationToken.None);

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

        var command = new GerarOrdemServicoCommand(ClienteId, VeiculoId);
        Result<OrdemServicoResumoDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.VeiculoInexistenteOuNaoPertenceAoCliente", result.Error.Code);
        _repoMock.Verify(x => x.Adicionar(It.IsAny<OrdensServico.Domain.OrdemServico.OrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
