using Cadastro.Adapters.DataSources;
using Cadastro.Domain.Cliente;
using Microsoft.EntityFrameworkCore;

namespace Cadastro.Infrastructure.Persistence.Repositories;

internal sealed class ClienteRepository : IClienteRepository
{
    private readonly CadastroDbContext _context;

    public ClienteRepository(CadastroDbContext context) => _context = context;

    public Task Adicionar(Cliente cliente, CancellationToken ct = default)
    {
        _context.Clientes.Add(cliente);
        return Task.CompletedTask;
    }

    public Task<Cliente?> ObterPorId(ClienteId id, CancellationToken ct = default) =>
        _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistePorDocumento(string documento, CancellationToken ct = default) =>
        _context.Database
            .SqlQuery<bool>($"SELECT EXISTS(SELECT 1 FROM cadastro.cliente WHERE documento = {documento}) AS \"Value\"")
            .FirstAsync(ct);

    public Task Atualizar(Cliente cliente, CancellationToken ct = default)
    {
        _context.Clientes.Update(cliente);
        return Task.CompletedTask;
    }
}
