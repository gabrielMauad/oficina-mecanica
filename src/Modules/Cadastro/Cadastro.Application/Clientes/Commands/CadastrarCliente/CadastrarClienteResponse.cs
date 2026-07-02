using Cadastro.Domain.Cliente;

namespace Cadastro.Application.Clientes.Commands.CadastrarCliente;

public sealed record CadastrarClienteResponse(
    Guid ClienteId,
    string Nome,
    string Documento,
    string Email,
    string Telefone,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static CadastrarClienteResponse FromCliente(Cliente cliente) => new(
        ClienteId: cliente.Id.Value,
        Nome: cliente.Nome,
        Documento: cliente.Documento.Numero,
        Email: cliente.Email,
        Telefone: cliente.Telefone,
        Ativo: cliente.Ativo,
        CadastradoEm: cliente.CadastradoEm,
        AtualizadoEm: cliente.AtualizadoEm
    );
}
