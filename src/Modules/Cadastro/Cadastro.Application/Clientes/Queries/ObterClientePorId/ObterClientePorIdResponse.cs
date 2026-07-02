namespace Cadastro.Application.Clientes.Queries.ObterClientePorId;

public sealed record ObterClientePorIdResponse(
    Guid Id,
    string Nome,
    string Documento,
    string Email,
    string Telefone,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
);
