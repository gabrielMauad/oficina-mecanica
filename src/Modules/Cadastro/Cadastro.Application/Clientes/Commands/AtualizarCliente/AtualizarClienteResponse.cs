using Cadastro.Domain.Cliente;

namespace Cadastro.Application.Clientes.Commands.AtualizarCliente;

public sealed record AtualizarClienteResponse(
    Guid ClienteId,
    string Nome,
    string Telefone,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static AtualizarClienteResponse FromCliente(Cliente cliente) => new(
        ClienteId: cliente.Id.Value,
        Nome: cliente.Nome,
        Telefone: cliente.Telefone,
        Ativo: cliente.Ativo,
        CadastradoEm: cliente.CadastradoEm,
        AtualizadoEm: cliente.AtualizadoEm
    );
}
