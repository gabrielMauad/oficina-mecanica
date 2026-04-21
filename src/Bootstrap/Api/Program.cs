using Api.Middlewares;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
// builder.Services.AddCadastroModule(builder.Configuration);
// builder.Services.AddOrdemServicoModule(builder.Configuration);
// builder.Services.AddPecasInsumosModule(builder.Configuration);

builder.Services.AddControllers();
// .AddApplicationPart(typeof(Cadastro.Presentation.CadastroAssemblyMarker).Assembly)
// .AddApplicationPart(typeof(OrdemServico.Presentation.OrdemServicoAssemblyMarker).Assembly)
// .AddApplicationPart(typeof(PecasInsumos.Presentation.PecasInsumosAssemblyMarker).Assembly);

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
app.Run();
