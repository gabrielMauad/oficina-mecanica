using Cadastro.Domain.Cliente;
using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Clientes.Commands.CadastrarCliente;

public sealed class CadastrarClienteHandler
    : IRequestHandler<CadastrarClienteCommand, Result<CadastrarClienteResponse>>
{
    private readonly IClienteRepository _repository;

    public CadastrarClienteHandler(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CadastrarClienteResponse>> Handle(
        CadastrarClienteCommand command,
        CancellationToken cancellationToken)
    {
        var documentoNormalizado = new string(command.Documento.Where(char.IsDigit).ToArray());

        if (await _repository.ExistePorDocumento(documentoNormalizado, cancellationToken))
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

        return CadastrarClienteResponse.FromCliente(cliente);
    }
}
