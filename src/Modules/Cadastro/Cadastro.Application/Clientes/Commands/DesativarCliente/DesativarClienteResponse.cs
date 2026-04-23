using Cadastro.Domain.Cliente;

namespace Cadastro.Application.Clientes.Commands.DesativarCliente;

public sealed record DesativarClienteResponse(
    Guid ClienteId,
    string Nome,
    bool Ativo
)
{
    public static DesativarClienteResponse FromCliente(Cliente cliente)
    {
        return new DesativarClienteResponse(
            cliente.Id.Value,
            cliente.Nome,
            cliente.Ativo
        );
    }
}

