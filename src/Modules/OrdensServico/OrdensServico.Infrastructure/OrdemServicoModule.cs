using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrdensServico.Application.Ordens.Commands.GerarOrdemServico;
using OrdensServico.Application.Ports;
using OrdensServico.Contracts.Queries;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Domain.Ports;
using OrdensServico.Infrastructure.Acl;
using OrdensServico.Infrastructure.Persistence;
using OrdensServico.Infrastructure.Persistence.Repositories;
using OrdensServico.Infrastructure.Queries;
using SharedKernel.Application;

namespace OrdensServico.Infrastructure;

public static class OrdemServicoModule
{
    public static IServiceCollection AddOrdensServicoModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrdensServicoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrdensServicoDbContext>());

        #region Repositórios de domínio

        services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();

        #endregion

        #region Adapters de ACL (ports → implementações externas)

        services.AddScoped<IClienteInfoPort, ClienteInfoAdapter>();
        services.AddScoped<IVeiculoInfoPort, VeiculoInfoAdapter>();
        services.AddScoped<IServicoInfoPort, ServicoInfoAdapter>();
        services.AddScoped<IPecaDisponibilidadePort, PecaDisponibilidadeAdapter>();
        services.AddScoped<IPecaInsumoInfoPort, PecaInsumoInfoAdapter>();
        services.AddScoped<INotificacaoClientePort, NotificacaoClienteAdapter>();

        #endregion

        #region Queries de leitura

        services.AddScoped<IListarOrdensPorClienteQuery, ListarOrdensPorClienteQueryImpl>();

        #endregion

        #region Queries públicas (contratos expostos aos outros módulos)

        services.AddScoped<IOrdemServicoResumoQuery, OrdemServicoResumoQuery>();

        #endregion

        var applicationAssembly = typeof(GerarOrdemServicoCommand).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
            cfg.AddOpenBehavior(typeof(SharedKernel.Application.Behaviors.LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(SharedKernel.Application.Behaviors.ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(SharedKernel.Application.Behaviors.TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
