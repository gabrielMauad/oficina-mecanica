using OrdensServico.Application.Gateways;
using OrdensServico.Application.Ordens.Queries.ObterOrdemServicoPorId;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Tests.Queries;

public class ObterOrdemServicoPorIdHandlerTests
{
    private readonly Mock<IOrdemServicoGateway> _repoMock = new();
    private readonly ObterOrdemServicoPorIdHandler _handler;

    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();

    public ObterOrdemServicoPorIdHandlerTests()
    {
        _handler = new(_repoMock.Object);
    }

    [Fact(DisplayName = "Encontrada: retorna entidade com ClienteId, VeiculoId e Status corretos")]
    public async Task Handle_OsEncontrada_RetornaEntidadeCorreta()
    {
        var os = OrdensServico.Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId).Value;
        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        var query = new ObterOrdemServicoPorIdQuery(os.Id.Value);
        Result<OrdensServico.Domain.OrdemServico.OrdemServico> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(os.Id.Value, result.Value.Id.Value);
        Assert.Equal(ClienteId, result.Value.ClienteId);
        Assert.Equal(VeiculoId, result.Value.VeiculoId);
        Assert.Equal("Recebida", result.Value.Status.ToString());
    }

    [Fact(DisplayName = "Não encontrada: retorna erro NaoEncontrada")]
    public async Task Handle_OsNaoEncontrada_RetornaErroNaoEncontrada()
    {
        _repoMock.Setup(x => x.ObterPorId(It.IsAny<OrdemServicoId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdensServico.Domain.OrdemServico.OrdemServico?)null);

        var query = new ObterOrdemServicoPorIdQuery(Guid.NewGuid());
        Result<OrdensServico.Domain.OrdemServico.OrdemServico> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.NaoEncontrada", result.Error.Code);
    }
}
