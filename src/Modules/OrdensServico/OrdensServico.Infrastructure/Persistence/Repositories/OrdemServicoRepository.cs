using Microsoft.EntityFrameworkCore;
using OrdensServico.Adapters.DataSources;
using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Infrastructure.Persistence.Repositories;

internal sealed class OrdemServicoRepository : IOrdemServicoRepository
{
    private readonly OrdensServicoDbContext _context;
    public OrdemServicoRepository(OrdensServicoDbContext context) => _context = context;
    public Task Adicionar(OrdemServico ordemServico, CancellationToken ct = default)
    {
        _context.OrdensServico.Add(ordemServico);
        return Task.CompletedTask;
    }
    public Task<OrdemServico?> ObterPorId(OrdemServicoId id, CancellationToken ct = default) =>
        _context.OrdensServico
            .Include(os => os.ItensServico)
            .Include(os => os.ItensPeca)
            .Include(os => os.Orcamentos)
            .FirstOrDefaultAsync(os => os.Id == id, ct);


    public Task Atualizar(OrdemServico ordemServico, CancellationToken ct = default)
    {
        _context.OrdensServico.Update(ordemServico);
        return Task.CompletedTask;
    }
}
