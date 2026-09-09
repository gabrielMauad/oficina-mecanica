using System.Diagnostics.Metrics;

namespace OrdensServico.Application.Metrics;

public sealed class OrdensServicoMetrics : IDisposable
{
    public const string MeterName = "OficinaMecanica.OrdensServico";
    public const string OrdensAbertasInstrumentName = "oficina.ordens_servico.abertas";
    public const string EtapaDuracaoInstrumentName = "oficina.ordens_servico.etapa.duracao";

    private readonly Meter _meter;
    private readonly Counter<long> _ordensAbertas;
    private readonly Histogram<double> _etapaDuracao;

    public OrdensServicoMetrics()
    {
        _meter = new Meter(MeterName);

        _ordensAbertas = _meter.CreateCounter<long>(
            OrdensAbertasInstrumentName,
            unit: "{ordem}",
            description: "Ordens de serviço abertas");

        _etapaDuracao = _meter.CreateHistogram<double>(
            EtapaDuracaoInstrumentName,
            unit: "s",
            description: "Duração de cada etapa da ordem de serviço");
    }

    public Meter Meter => _meter;

    public void RegistrarOrdemAberta(string tipo) =>
        _ordensAbertas.Add(1, new KeyValuePair<string, object?>("tipo", tipo));

    public void RegistrarDuracaoEtapa(string etapa, double duracaoEmSegundos) =>
        _etapaDuracao.Record(duracaoEmSegundos, new KeyValuePair<string, object?>("etapa", etapa));

    public void Dispose() => _meter.Dispose();
}
