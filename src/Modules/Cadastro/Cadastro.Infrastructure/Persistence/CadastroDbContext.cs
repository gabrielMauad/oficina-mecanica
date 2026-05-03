using Cadastro.Domain.Cliente;
using Cadastro.Domain.Servico;
using Cadastro.Domain.Veiculo;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application;

namespace Cadastro.Infrastructure.Persistence;

public sealed class CadastroDbContext : DbContext, IUnitOfWork
{
    public CadastroDbContext(DbContextOptions<CadastroDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Servico> Servicos => Set<Servico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cadastro");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CadastroDbContext).Assembly);
    }
}
