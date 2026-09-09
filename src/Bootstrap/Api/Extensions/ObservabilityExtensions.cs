using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Api.Extensions;

/// <summary>
/// Configura o SDK do OpenTelemetry (traces + métricas), exportado via OTLP (ADR-004). O
/// destino é resolvido pelas variáveis de ambiente padrão do OpenTelemetry
/// (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, <c>OTEL_EXPORTER_OTLP_HEADERS</c>), lidas
/// automaticamente pelo <see cref="OpenTelemetry.Exporter.OtlpExporterOptions"/> — não há
/// nada específico de fornecedor (New Relic/Datadog/etc.) neste código, para que a escolha do
/// APM continue em aberto.
/// </summary>
public static class ObservabilityExtensions
{
    // Meters de negócio criados por outra tarefa (em paralelo), registrados aqui apenas pelo
    // nome — não exige referência às classes que os instanciam.
    private static readonly string[] ApplicationMeterNames =
    [
        "OficinaMecanica.OrdensServico",
        "OficinaMecanica.Integracoes"
    ];

    public static WebApplicationBuilder AddOficinaMecanicaObservability(this WebApplicationBuilder builder)
    {
        // No ambiente "Testing" (WebApplicationFactory dos testes de integração) não existe
        // coletor OTLP disponível: manter o SDK fora do host evita tentativas de exportação
        // (retries/timeouts) e mantém a suíte de testes rápida e determinística.
        if (builder.Environment.IsEnvironment("Testing"))
            return builder;

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: "oficina-mecanica-api", serviceNamespace: "oficina-mecanica")
            .AddAttributes(
            [
                new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
            ]);

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resourceBuilder)
                // ADR-004: amostragem em 100% neste projeto, para que nenhum trace usado como
                // evidência (vídeo de entrega) seja descartado. Deliberado para o volume deste
                // projeto — NÃO seria adequado em um ambiente de produção real de alto tráfego,
                // onde amostragem parcial é necessária para controlar custo/volume.
                .SetSampler(new AlwaysOnSampler())
                .AddAspNetCoreInstrumentation() // requisições de entrada
                .AddHttpClientInstrumentation() // chamadas HTTP de saída
                .AddNpgsql() // comandos SQL executados via Npgsql/EF Core, aninhados no span da requisição
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation() // latência das APIs
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation() // CPU, memória e GC do processo
                .AddMeter(ApplicationMeterNames)
                .AddOtlpExporter());

        return builder;
    }
}
