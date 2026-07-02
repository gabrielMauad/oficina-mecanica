using Moq;
using PecasInsumos.Application.Commands.IncrementarEstoque;
using PecasInsumos.Domain;
using SharedKernel.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Application.Tests.Commands;

public class IncrementarEstoqueHandlerTests
{
    private readonly Mock<IPecaInsumoRepository> _repositoryMock;
    private readonly IncrementarEstoqueHandler _handler;

    public IncrementarEstoqueHandlerTests()
    {
        _repositoryMock = new Mock<IPecaInsumoRepository>();
        _handler = new IncrementarEstoqueHandler(
            _repositoryMock.Object
        );
    }

    [Fact(DisplayName = "Cenário feliz")]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new IncrementarEstoqueCommand(
            PecaInsumoId: Guid.NewGuid(),
            Quantidade: 5
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição", 10, 3, UnidadeDeMedida.Unidade).Value;
        var quantidadeEsperada = pecaInsumo.QuantidadeEmEstoque + command.Quantidade;

        _repositoryMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);

        Assert.NotEqual(Guid.Empty, result.Value.PecaInsumoId);
        Assert.Equal(pecaInsumo.Nome, result.Value.Nome);
        Assert.Equal(quantidadeEsperada, result.Value.QuantidadeEmEstoque);
        Assert.True(result.Value.Ativo);
        Assert.InRange(result.Value.AtualizadoEm, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);

        _repositoryMock.Verify(x => x.Atualizar(It.Is<PecaInsumoEntity>(p => p.QuantidadeEmEstoque == quantidadeEsperada), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: Peça/insumo não encontrada")]
    public async Task Handle_ShouldReturnError_WhenPecaInsumoNotFound()
    {
        // Arrange
        var command = new IncrementarEstoqueCommand(
            PecaInsumoId: Guid.NewGuid(),
            Quantidade: 5
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.NaoEncontrada", result.Error.Code);
        Assert.Equal("Peça/insumo não encontrada.", result.Error.Message);
    }

    [Fact(DisplayName = "Erro: Peça/insumo desativada")]
    public async Task Handle_ShouldReturnError_WhenPecaInsumoDesativada()
    {
        // Arrange
        var command = new IncrementarEstoqueCommand(
            PecaInsumoId: Guid.NewGuid(),
            Quantidade: 5
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição", 10, 3, UnidadeDeMedida.Unidade).Value;
        pecaInsumo.Desativar();
        _repositoryMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.JaDesativada", result.Error.Code);
        Assert.Equal("A peça/insumo já está desativada.", result.Error.Message);
    }

    [Fact(DisplayName = "Erro: Replica erro de domínio")]
    public async Task Handle_ShouldReturnError_WhenDomainFails()
    {
        // Arrange
        var command = new IncrementarEstoqueCommand(
            PecaInsumoId: Guid.NewGuid(),
            Quantidade: 0
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição", 10, 3, UnidadeDeMedida.Unidade).Value;
        _repositoryMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.QuantidadeInvalida", result.Error.Code);
        Assert.Equal("Quantidade a incrementar deve ser positiva.", result.Error.Message);
    }
}
