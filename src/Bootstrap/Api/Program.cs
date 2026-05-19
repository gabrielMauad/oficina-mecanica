using Api.Middlewares;
using Cadastro.Infrastructure;
using Cadastro.Infrastructure.Persistence;
using Cadastro.Presentation;
using Microsoft.EntityFrameworkCore;
using PecasInsumos.Infrastructure;
using PecasInsumos.Infrastructure.Persistence;
using PecasInsumos.Presentation;
using Scalar.AspNetCore;
using SharedKernel.Application;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddCadastroModule(builder.Configuration);
builder.Services.AddSharedKernelServices();
builder.Services.AddPecasInsumosModule(builder.Configuration);
// builder.Services.AddOrdemServicoModule(builder.Configuration);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(CadastroAssemblyMarker).Assembly)
    .AddApplicationPart(typeof(PecasInsumosAssemblyMarker).Assembly);
// .AddApplicationPart(typeof(OrdensServico.Presentation.OrdensServicoAssemblyMarker).Assembly)

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
// app.UseAuthorization();
app.MapControllers();
// Configure the HTTP request pipeline for DEVELOPMENT only
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(option =>
    {
        option.Title = "Oficina Mecanica API";
    });

    // Automatically redirect to Scalar documentation
    app.MapGet("/", () => Results.Redirect("/scalar"));
}
app.MapHealthChecks("/healthz");

using (var scope = app.Services.CreateScope())
{
    var cadastroDb = scope.ServiceProvider.GetRequiredService<CadastroDbContext>();
    if (cadastroDb.Database.IsRelational())
        await cadastroDb.Database.MigrateAsync();

    var pecasInsumoDb = scope.ServiceProvider.GetRequiredService<PecasInsumosDbContext>();
    if (pecasInsumoDb.Database.IsRelational())
        await pecasInsumoDb.Database.MigrateAsync();
}

await app.RunAsync();
