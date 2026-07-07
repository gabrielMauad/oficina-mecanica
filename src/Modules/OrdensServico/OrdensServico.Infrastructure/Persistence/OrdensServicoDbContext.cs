using Microsoft.EntityFrameworkCore;
using OrdensServico.Adapters.DataSources.Records;
using SharedKernel.Application;

namespace OrdensServico.Infrastructure.Persistence;

public sealed class OrdensServicoDbContext : DbContext, IUnitOfWork
{
    public OrdensServicoDbContext(DbContextOptions<OrdensServicoDbContext> options) : base(options) { }

    public DbSet<OrdemServicoRecord> OrdensServico => Set<OrdemServicoRecord>();
    public DbSet<ItemPecaRecord> ItemPecas => Set<ItemPecaRecord>();
    public DbSet<ItemServicoRecord> ItemServicos => Set<ItemServicoRecord>();
    public DbSet<OrcamentoRecord> Orcamentos => Set<OrcamentoRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordem_servico");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdensServicoDbContext).Assembly);
    }
}
