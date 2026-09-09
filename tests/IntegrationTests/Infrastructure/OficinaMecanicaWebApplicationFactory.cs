using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
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

    // Mesmos valores configurados via Jwt:Issuer / Jwt:Audience / Jwt:ValidIssuers abaixo —
    // ver RFC-001 §4.1 (contrato do token) e ADR-003 (dois emissores).
    private const string AppIssuer = "oficina-mecanica-app";
    private const string ClienteAuthIssuer = "oficina-mecanica-auth";
    private const string Audience = "oficina-mecanica-api";

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
                ["Jwt:Issuer"] = AppIssuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:ValidIssuers:0"] = AppIssuer,
                ["Jwt:ValidIssuers:1"] = ClienteAuthIssuer,
                // Credenciais do admin — usadas pelo LoginHandler
                ["Auth:AdminEmail"] = TestAdminEmail,
                ["Auth:AdminSenha"] = TestAdminSenha,
                // A exposição do OpenAPI/Scalar não depende mais do ambiente (ver
                // Api.Extensions.OpenApiExtensions), então é desligada explicitamente aqui
                // para manter a suíte de integração rápida e sem rotas de documentação.
                ["OpenApi:Enabled"] = "false",
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

    /// <summary>
    /// Gera um token de papel Cliente assinado com o mesmo segredo de teste, simulando o
    /// token que a Function Serverless (emissor "oficina-mecanica-auth") emitiria — ver
    /// RFC-001 §4.1. A aplicação não emite esse token, então é montado diretamente aqui.
    /// </summary>
    public string GetClienteAuthToken(Guid clienteId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, clienteId.ToString()),
            new Claim("role", "Cliente")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: ClienteAuthIssuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Cria um HttpClient autenticado com um token de papel Cliente para o clienteId informado.
    /// </summary>
    public HttpClient CreateClienteAuthenticatedClient(Guid clienteId) =>
        CreateAuthenticatedClient(GetClienteAuthToken(clienteId));

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
