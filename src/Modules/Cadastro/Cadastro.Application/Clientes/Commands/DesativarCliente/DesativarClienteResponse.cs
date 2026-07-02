using Cadastro.Domain.Cliente;

namespace Cadastro.Application.Clientes.Commands.DesativarCliente;

public sealed record DesativarClienteResponse(
    Guid ClienteId,
    string Nome,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static DesativarClienteResponse FromCliente(Cliente cliente)
    {
        return new DesativarClienteResponse(
            cliente.Id.Value,
            cliente.Nome,
            cliente.Ativo,
            cliente.CadastradoEm,
            cliente.AtualizadoEm
        );
    }
}

