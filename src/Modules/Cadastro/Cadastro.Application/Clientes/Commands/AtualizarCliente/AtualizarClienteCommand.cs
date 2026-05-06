using SharedKernel.Application;

namespace Cadastro.Application.Clientes.Commands.AtualizarCliente;

public sealed record AtualizarClienteCommand(
    Guid Id,
    string? Nome,
    string? Telefone
) : ICommand<AtualizarClienteResponse>;

