using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// Factory compartilhada por todos os testes de integração via [Collection("Integration")].
/// Sobe um container PostgreSQL real (Testcontainers) uma única vez por sessão de testes,
/// aplica migrations automaticamente (via Program.cs que já chama MigrateAsync no startup),
/// e fornece helpers de autenticação.
/// </summary>
public sealed class OficinaMecanicaWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Credenciais de teste — mesmas injetadas na configuração do host
    private const string TestJwtSecret = "integration-test-jwt-secret-minimum-32-chars!!";
    private const string TestAdminEmail = "admin@integration.test";
    private const string TestAdminSenha = "Test@Integration1234";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithDatabase("oficina_mecanica")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    // IAsyncLifetime.InitializeAsync — xunit.v3 chama antes do primeiro teste da coleção
    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();
    }

    // Sobrescreve WebApplicationFactory.DisposeAsync e também satisfaz IAsyncLifetime.DisposeAsync
    // (ambos têm a mesma assinatura ValueTask DisposeAsync())
    public override async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" evita que app.MapOpenApi() e Scalar sejam registrados
        builder.UseEnvironment("Testing");

        // Sobrescreve configurações sensíveis com valores de teste.
        // ConfigureAppConfiguration adiciona fontes APÓS os defaults (appsettings.json),
        // então estes valores têm prioridade.
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Connection string aponta para o container Testcontainers
                ["ConnectionStrings:Default"] = _db.GetConnectionString(),
                // JWT secret — mesmo valor usado para gerar e validar tokens nos testes
                ["Jwt:Secret"] = TestJwtSecret,
                // Credenciais do admin — usadas pelo LoginHandler
                ["Auth:AdminEmail"] = TestAdminEmail,
                ["Auth:AdminSenha"] = TestAdminSenha,
            });
        });
    }

    /// <summary>
    /// Cria um HttpClient com Authorization: Bearer {token} pré-configurado.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Autentica no endpoint POST /api/v1/auth/login com as credenciais de teste
    /// e retorna o JWT token.
    /// </summary>
    public async Task<string> GetAuthTokenAsync()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = TestAdminEmail,
            Senha = TestAdminSenha
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }

    private sealed record LoginResponseDto(string Token, DateTime ExpiresAt);
}

/// <summary>
/// Define a coleção "Integration" — todos os testes com [Collection("Integration")]
/// compartilham a mesma instância de OficinaMecanicaWebApplicationFactory.
/// xunit.v3 garante que os testes da coleção NÃO rodam em paralelo entre si.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection
    : ICollectionFixture<OficinaMecanicaWebApplicationFactory> { }
