using Cadastro.Domain.Cliente;
using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Clientes.Queries.ObterClientePorId;

public sealed class ObterClientePorIdHandler
    : IRequestHandler<ObterClientePorIdQuery, Result<ObterClientePorIdResponse>>
{
    private readonly IClienteRepository _repository;

    public ObterClientePorIdHandler(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ObterClientePorIdResponse>> Handle(
        ObterClientePorIdQuery query,
        CancellationToken cancellationToken)
    {
        var cliente = await _repository.ObterPorId(
            new ClienteId(query.ClienteId),
            cancellationToken);

        if (cliente is null)
            return ClienteErrors.NaoEncontrado;

        return new ObterClientePorIdResponse(
            Id: cliente.Id.Value,
            Nome: cliente.Nome,
            Documento: cliente.Documento.Numero,
            Email: cliente.Email,
            Telefone: cliente.Telefone,
            Ativo: cliente.Ativo,
            CadastradoEm: cliente.CadastradoEm,
            AtualizadoEm: cliente.AtualizadoEm
        );
    }
}
