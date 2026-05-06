using Cadastro.Contracts.Dtos;
using Cadastro.Contracts.Queries;
using Cadastro.Domain.Cliente;
using Cadastro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cadastro.Infrastructure.Queries;

internal sealed class CadastroClienteQuery : ICadastroClienteQuery
{
    private readonly CadastroDbContext _context;

    public CadastroClienteQuery(CadastroDbContext context) => _context = context;

    public async Task<ClienteDto?> ObterPorId(Guid id, CancellationToken ct = default)
    {
        var clienteId = new ClienteId(id);
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clienteId, ct);

        if (cliente is null)
            return null;

        return new ClienteDto(
            cliente.Id.Value,
            cliente.Nome,
            cliente.Documento.Numero,
            cliente.Email,
            cliente.Ativo);
    }
}
