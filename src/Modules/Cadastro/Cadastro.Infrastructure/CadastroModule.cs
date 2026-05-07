using Cadastro.Application.Clientes.Commands.CadastrarCliente;
using Cadastro.Application.Clientes.Queries.ListarClientes;
using Cadastro.Application.Servicos.Queries.ListarServicos;
using Cadastro.Application.Veiculos.Queries.ListarVeiculos;
using Cadastro.Application.Veiculos.Queries.ListarVeiculosPorCliente;
using Cadastro.Contracts.Queries;
using Cadastro.Domain.Cliente;
using Cadastro.Domain.Servico;
using Cadastro.Domain.Veiculo;
using Cadastro.Infrastructure.Persistence;
using Cadastro.Infrastructure.Persistence.Repositories;
using Cadastro.Infrastructure.Queries;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Application;

namespace Cadastro.Infrastructure;

public static class CadastroModule
{
    public static IServiceCollection AddCadastroModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CadastroDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CadastroDbContext>());

        #region Repositórios de domínio 

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();

        #endregion

        #region Queries de leitura

        services.AddScoped<IListarClientesQuery, ListarClientesQueryImpl>();
        services.AddScoped<IListarServicosQuery, ListarServicosQueryImpl>();
        services.AddScoped<IListarVeiculosQuery, ListarVeiculosQueryImpl>();
        services.AddScoped<IListarVeiculosPorClienteQuery, ListarVeiculosPorClienteQueryImpl>();

        #endregion

        #region Queries públicas (contratos expostos aos outros módulos)

        services.AddScoped<ICadastroClienteQuery, CadastroClienteQuery>();
        services.AddScoped<ICadastroServicoQuery, CadastroServicoQuery>();
        services.AddScoped<ICadastroVeiculoQuery, CadastroVeiculoQuery>();

        #endregion

        var applicationAssembly = typeof(CadastrarClienteCommand).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
