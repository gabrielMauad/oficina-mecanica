using Microsoft.EntityFrameworkCore;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Application;
using SharedKernel.Domain;

namespace OrdensServico.Infrastructure.Persistence;

public sealed class OrdensServicoDbContext : DbContext, IUnitOfWork
{
    public OrdensServicoDbContext(DbContextOptions<OrdensServicoDbContext> options) : base(options) { }

    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<ItemPeca> ItemPecas => Set<ItemPeca>();
    public DbSet<ItemServico> ItemServicos => Set<ItemServico>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordem_servico");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdensServicoDbContext).Assembly);
    }

    public IReadOnlyList<IDomainEvent> CollectDomainEvents() =>
        ChangeTracker.Entries<IHasDomainEvents>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

    public void ClearDomainEvents()
    {
        foreach (var entry in ChangeTracker.Entries<IHasDomainEvents>())
            entry.Entity.ClearDomainEvents();
    }
}
