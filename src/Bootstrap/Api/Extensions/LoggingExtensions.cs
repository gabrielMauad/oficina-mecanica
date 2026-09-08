using Api.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Api.Extensions;

public static class LoggingExtensions
{
    /// <summary>
    /// Substitui o formatter de console padrão por um formatter JSON estruturado que
    /// carrega trace_id/span_id (Activity.Current) em cada linha, conforme ADR-004.
    /// </summary>
    public static WebApplicationBuilder AddStructuredJsonLogging(this WebApplicationBuilder builder)
    {
        builder.Logging
            .AddConsole(options => options.FormatterName = TraceJsonConsoleFormatter.FormatterName)
            .AddConsoleFormatter<TraceJsonConsoleFormatter, ConsoleFormatterOptions>();

        return builder;
    }
}
