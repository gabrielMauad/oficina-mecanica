using OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Contracts.Queries;
using SharedKernel.Domain;

namespace OrdensServico.Application.Tests.Queries;

public class ListarOrdensPorClienteHandlerTests
{
    private readonly Mock<IListarOrdensPorClienteQuery> _queryMock = new();
    private readonly ListarOrdensPorClienteHandler _handler;

    private static readonly Guid ClienteId = Guid.NewGuid();

    public ListarOrdensPorClienteHandlerTests()
    {
        _handler = new(_queryMock.Object);
    }

    [Fact(DisplayName = "Lista com ordens: retorna lista com os itens corretos")]
    public async Task Handle_HaOrdensParaOCliente_RetornaListaComItens()
    {
        var ordens = new List<OrdemServicoResumoDto>
        {
            new(Guid.NewGuid(), ClienteId, Guid.NewGuid(), "Recebida", null, null, null, DateTime.UtcNow, DateTime.UtcNow, [], [], []),
            new(Guid.NewGuid(), ClienteId, Guid.NewGuid(), "EmDiagnostico", null, null, null, DateTime.UtcNow, DateTime.UtcNow, [], [], [])
        };
        _queryMock.Setup(x => x.Listar(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ordens);

        var query = new ListarOrdensPorClienteQuery(ClienteId);
        Result<List<OrdemServicoResumoDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, o => Assert.Equal(ClienteId, o.ClienteId));
    }

    [Fact(DisplayName = "Sem ordens: retorna lista vazia com sucesso")]
    public async Task Handle_NaoHaOrdensParaOCliente_RetornaListaVazia()
    {
        _queryMock.Setup(x => x.Listar(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrdemServicoResumoDto>());

        var query = new ListarOrdensPorClienteQuery(ClienteId);
        Result<List<OrdemServicoResumoDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
