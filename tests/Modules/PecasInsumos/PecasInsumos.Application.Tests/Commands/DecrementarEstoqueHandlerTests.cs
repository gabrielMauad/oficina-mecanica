using Moq;
using PecasInsumos.Application.Commands.DecrementarEstoque;
using PecasInsumos.Contracts.IntegrationEvents;
using PecasInsumos.Domain;
using SharedKernel.Application;
using SharedKernel.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Application.Tests.Commands;

public class DecrementarEstoqueHandlerTests
{
    private readonly Mock<IPecaInsumoRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventBus> _busMock;
    private readonly Mock<IPendingIntegrationEvents> _pendingEventsMock;
    private readonly DecrementarEstoqueHandler _handler;

    public DecrementarEstoqueHandlerTests()
    {
        _repositoryMock = new Mock<IPecaInsumoRepository>();
        _busMock = new Mock<IIntegrationEventBus>();
        _pendingEventsMock = new Mock<IPendingIntegrationEvents>();
        _handler = new DecrementarEstoqueHandler(
            _repositoryMock.Object,
            _busMock.Object,
            _pendingEventsMock.Object
        );
    }

    [Fact(DisplayName = "Cenário feliz")]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new DecrementarEstoqueCommand(
            PecaInsumoId: Guid.NewGuid(),
            Quantidade: 3
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição", 10, 10, UnidadeDeMedida.Unidade).Value;
        var quantidadeEsperada = pecaInsumo.QuantidadeEmEstoque - command.Quantidade;

        _repositoryMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        Func<CancellationToken, Task>? enqueuedAction = null;
        _pendingEventsMock
            .Setup(x => x.Enqueue(It.IsAny<Func<CancellationToken, Task>>()))
            .Callback<Func<CancellationToken, Task>>(action => enqueuedAction = action);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        await enqueuedAction!(CancellationToken.None);

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
        _busMock.Verify(x => x.Publish(It.Is<EstoqueDecrementadoIntegrationEvent>(e => e.QuantidadeDecrementada == command.Quantidade && e.QuantidadeRestante == quantidadeEsperada), It.IsAny<CancellationToken>()), Times.Once);
        _pendingEventsMock.Verify(x => x.Enqueue(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: Peça/insumo não encontrada")]
    public async Task Handle_ShouldReturnError_WhenPecaInsumoNotFound()
    {
        // Arrange
        var command = new DecrementarEstoqueCommand(
            PecaInsumoId: Guid.NewGuid(),
            Quantidade: 3
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
        var command = new DecrementarEstoqueCommand(
            PecaInsumoId: Guid.NewGuid(),
            Quantidade: 3
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição", 10, 10, UnidadeDeMedida.Unidade).Value;
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

    [Fact(DisplayName = "Erro: Replica erro de domínio - quantidade inválida")]
    public async Task Handle_ShouldReturnError_WhenQuantidadeIsZero()
    {
        // Arrange
        var command = new DecrementarEstoqueCommand(
            PecaInsumoId: Guid.NewGuid(),
            Quantidade: 0
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição", 10, 10, UnidadeDeMedida.Unidade).Value;
        _repositoryMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.QuantidadeInvalida", result.Error.Code);
        Assert.Equal("Quantidade a decrementar deve ser positiva.", result.Error.Message);
    }

    [Fact(DisplayName = "Erro: Replica erro de domínio - estoque insuficiente")]
    public async Task Handle_ShouldReturnError_WhenEstoqueInsuficiente()
    {
        // Arrange
        var command = new DecrementarEstoqueCommand(
            PecaInsumoId: Guid.NewGuid(),
            Quantidade: 20
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição", 10, 5, UnidadeDeMedida.Unidade).Value;
        _repositoryMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.EstoqueInsuficiente", result.Error.Code);
        Assert.Equal("Quantidade em estoque não pode ficar negativa.", result.Error.Message);
    }
}
