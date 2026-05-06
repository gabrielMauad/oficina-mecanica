using Cadastro.Domain.Cliente;
using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Clientes.Commands.DesativarCliente;

public sealed class DesativarClienteHandler : IRequestHandler<DesativarClienteCommand, Result<DesativarClienteResponse>>
{
    private readonly IClienteRepository _repository;

    public DesativarClienteHandler(IClienteRepository repository) => _repository = repository;

    public async Task<Result<DesativarClienteResponse>> Handle(DesativarClienteCommand command, CancellationToken cancellationToken)
    {
        ClienteId clienteId = new ClienteId(command.ClienteId);
        Cliente? cliente = await _repository.ObterPorId(clienteId, cancellationToken);
        if (cliente is null)
            return ClienteErrors.NaoEncontrado;
        if (!cliente.Ativo)
            return ClienteErrors.JaDesativado;
        cliente.Desativar();
        await _repository.Atualizar(cliente, cancellationToken);

        return DesativarClienteResponse.FromCliente(cliente);
    }

}

