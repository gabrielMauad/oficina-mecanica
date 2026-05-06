using Cadastro.Application.Clientes.Commands.CadastrarCliente;
using Cadastro.Contracts.IntegrationEvents;
using Cadastro.Domain.Cliente;
using Moq;
using SharedKernel.Application;
using SharedKernel.Domain;
using ClienteEntity = Cadastro.Domain.Cliente.Cliente;

namespace Cadastro.Application.Tests.Cliente.Commands;

public class CadastrarClienteHandlerTests
{
    private readonly Mock<IClienteRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventBus> _busMock;
    private readonly Mock<IPendingIntegrationEvents> _pendingEventsMock;
    private readonly CadastrarClienteHandler _handler;

    public CadastrarClienteHandlerTests()
    {
        _repositoryMock = new Mock<IClienteRepository>();
        _busMock = new Mock<IIntegrationEventBus>();
        _pendingEventsMock = new Mock<IPendingIntegrationEvents>();
        _handler = new CadastrarClienteHandler(
            _repositoryMock.Object,
            _busMock.Object,
            _pendingEventsMock.Object
        );
    }

    [Fact(DisplayName = "Cenário feliz")]
    public async Task Handle_ShouldReturnSucess_WhenCommandIsValid()
    {
        // Arrange
        var command = new CadastrarClienteCommand(
            Nome: "Cliente 1",
            Documento: "01404238000",
            Email: "cliente1@email.com",
            Telefone: "31999999999",
            PessoaFisica: true
        );

        _repositoryMock.Setup(x => x.ExistePorDocumento(command.Documento, It.IsAny<CancellationToken>())).ReturnsAsync(false);

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

        Assert.NotEqual(Guid.Empty, result.Value.ClienteId);
        Assert.Equal(command.Nome, result.Value.Nome);
        Assert.Equal(command.Documento, result.Value.Documento);
        Assert.Equal(command.Email, result.Value.Email);
        Assert.Equal(command.Telefone, result.Value.Telefone);
        Assert.True(result.Value.Ativo);
        Assert.InRange(result.Value.CadastradoEm, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
        Assert.InRange(result.Value.AtualizadoEm, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);

        _repositoryMock.Verify(x => x.Adicionar(It.Is<ClienteEntity>(x => x.Documento.Numero == command.Documento), It.IsAny<CancellationToken>()), Times.Once);
        _busMock.Verify(x => x.Publish(It.Is<ClienteCadastradoIntegrationEvent>(x => x.Nome == command.Nome), It.IsAny<CancellationToken>()), Times.Once);
        _pendingEventsMock.Verify(x => x.Enqueue(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: Documento ja existe")]
    public async Task Handle_ShouldReturnError_WhenDocumentoAlreadyExists()
    {
        // Arrange
        var command = new CadastrarClienteCommand(
            Nome: "Cliente 1",
            Documento: "01404238000",
            Email: "cliente1@email.com",
            Telefone: "31999999999",
            PessoaFisica: true
        );

        _repositoryMock.Setup(x => x.ExistePorDocumento(command.Documento, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Cliente.DocumentoJaExiste", result.Error.Code);
        Assert.Equal("Já existe um cliente cadastrado com este documento.", result.Error.Message);
    }

    [Fact(DisplayName = "Replica erro do dominio")]
    public async Task Handle_ShouldReturnError_WhenClienteFails()
    {
        // Arrange
        var command = new CadastrarClienteCommand(
            Nome: "Cliente 1",
            Documento: "123456678910",
            Email: "cliente1@email.com",
            Telefone: "31999999999",
            PessoaFisica: true
        );

        _repositoryMock.Setup(x => x.ExistePorDocumento(command.Documento, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("CPF.Invalido", result.Error.Code);
        Assert.Equal("CPF inválido.", result.Error.Message);
    }
}

