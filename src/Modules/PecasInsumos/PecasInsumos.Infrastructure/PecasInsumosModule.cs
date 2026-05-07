using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PecasInsumos.Application.Commands.AdicionarPecaInsumo;
using PecasInsumos.Application.Queries.ListarPecasInsumos;
using PecasInsumos.Contracts.Queries;
using PecasInsumos.Domain;
using PecasInsumos.Infrastructure.Persistence;
using PecasInsumos.Infrastructure.Queries;
using SharedKernel.Application;

namespace PecasInsumos.Infrastructure;

public static class PecasInsumosModule
{
    public static IServiceCollection AddPecasInsumosModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PecasInsumosDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PecasInsumosDbContext>());

        #region Repositórios de domínio 

        services.AddScoped<IPecaInsumoRepository, PecaInsumoRepository>();

        #endregion

        #region Queries de leitura

        services.AddScoped<IListarPecasInsumosQuery, ListarPecasInsumosQueryImpl>();

        #endregion

        #region Queries públicas (contratos expostos aos outros módulos)

        services.AddScoped<IPecaInsumoQuery, PecaInsumoQuery>();
        services.AddScoped<IPecasInsumosDisponibilidadeQuery, PecasInsumosDisponibilidadeQuery>();

        #endregion

        var applicationAssembly = typeof(AdicionarPecaInsumoCommand).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
