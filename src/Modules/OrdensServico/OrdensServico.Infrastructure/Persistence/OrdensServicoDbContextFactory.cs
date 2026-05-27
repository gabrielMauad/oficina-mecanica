using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrdensServico.Infrastructure.Persistence;

internal sealed class OrdensServicoDbContextFactory : IDesignTimeDbContextFactory<OrdensServicoDbContext>
{
    public OrdensServicoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        var options = new DbContextOptionsBuilder<OrdensServicoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OrdensServicoDbContext(options);
    }
}
