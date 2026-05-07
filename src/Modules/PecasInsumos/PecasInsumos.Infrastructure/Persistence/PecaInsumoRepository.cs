using Microsoft.EntityFrameworkCore;
using PecasInsumos.Domain;

namespace PecasInsumos.Infrastructure.Persistence;

internal sealed class PecaInsumoRepository : IPecaInsumoRepository
{
    private readonly PecasInsumosDbContext _context;
    public PecaInsumoRepository(PecasInsumosDbContext context) => _context = context;

    public Task Adicionar(PecaInsumo pecaInsumo, CancellationToken ct = default)
    {
        _context.PecasInsumos.Add(pecaInsumo);
        return Task.CompletedTask;
    }

    public Task<PecaInsumo?> ObterPorId(PecaInsumoId id, CancellationToken ct = default) =>
        _context.PecasInsumos.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExistePorNome(string nome, CancellationToken ct = default) =>
        _context.PecasInsumos.AnyAsync(s => s.Nome == nome, ct);

    public Task Atualizar(PecaInsumo pecaInsumo, CancellationToken ct = default)
    {
        _context.PecasInsumos.Update(pecaInsumo);
        return Task.CompletedTask;
    }
}

