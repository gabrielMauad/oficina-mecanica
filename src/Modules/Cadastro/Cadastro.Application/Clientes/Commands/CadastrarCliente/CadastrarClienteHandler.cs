using Cadastro.Contracts.IntegrationEvents;
using Cadastro.Domain.Cliente;
using MediatR;
using SharedKernel.Application;
using SharedKernel.Domain;

namespace Cadastro.Application.Clientes.Commands.CadastrarCliente;

public sealed class CadastrarClienteHandler
    : IRequestHandler<CadastrarClienteCommand, Result<CadastrarClienteResponse>>
{
    private readonly IClienteRepository _repository;
    private readonly IIntegrationEventBus _bus;
    private readonly IPendingIntegrationEvents _pendingEvents;

    public CadastrarClienteHandler(
        IClienteRepository repository,
        IIntegrationEventBus bus,
        IPendingIntegrationEvents pendingEvents)
    {
        _repository = repository;
        _bus = bus;
        _pendingEvents = pendingEvents;
    }

    public async Task<Result<CadastrarClienteResponse>> Handle(
        CadastrarClienteCommand command,
        CancellationToken cancellationToken)
    {
        if (await _repository.ExistePorDocumento(command.Documento, cancellationToken))
            return ClienteErrors.DocumentoJaExiste;

        var result = Cliente.Criar(
            command.Nome,
            command.Documento,
            command.Email,
            command.Telefone,
            command.PessoaFisica);

        if (result.IsFailure)
            return result.Error;

        var cliente = result.Value;

        await _repository.Adicionar(cliente, cancellationToken);

        _pendingEvents.Enqueue(ct => _bus.Publish(
            new ClienteCadastradoIntegrationEvent(
                EventId: Guid.NewGuid(),
                ClienteId: cliente.Id.Value,
                Nome: cliente.Nome,
                OcorridoEm: DateTime.UtcNow),
            ct));

        return new CadastrarClienteResponse(cliente.Id.Value);
    }
}
