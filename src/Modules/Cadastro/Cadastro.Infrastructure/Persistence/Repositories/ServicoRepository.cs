using Cadastro.Domain.Servico;
using Microsoft.EntityFrameworkCore;

namespace Cadastro.Infrastructure.Persistence.Repositories;

internal sealed class ServicoRepository : IServicoRepository
{
    private readonly CadastroDbContext _context;

    public ServicoRepository(CadastroDbContext context) => _context = context;

    public Task Adicionar(Servico servico, CancellationToken ct = default)
    {
        _context.Servicos.Add(servico);
        return Task.CompletedTask;
    }

    public Task<Servico?> ObterPorId(ServicoId id, CancellationToken ct = default) =>
        _context.Servicos.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExistePorNome(string nome, CancellationToken ct = default) =>
        _context.Servicos.AnyAsync(s => s.Nome == nome, ct);

    public Task Atualizar(Servico servico, CancellationToken ct = default)
    {
        _context.Servicos.Update(servico);
        return Task.CompletedTask;
    }
}
