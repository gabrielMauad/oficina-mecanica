using OrdensServico.Application.Ordens.Commands.RegistrarDiagnostico;

namespace OrdensServico.Adapters.Models.Request;

public sealed record RegistrarDiagnosticoRequest(
    string DescricaoDiagnostico,
    IReadOnlyList<ServicoItem> Servicos,
    IReadOnlyList<PecaItem> Pecas
);
