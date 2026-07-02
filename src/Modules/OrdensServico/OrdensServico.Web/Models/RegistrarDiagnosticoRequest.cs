using OrdensServico.Application.Ordens.Commands.RegistrarDiagnostico;

namespace OrdensServico.Web.Models;

public sealed record RegistrarDiagnosticoRequest(
    string DescricaoDiagnostico,
    IReadOnlyList<ServicoItem> Servicos,
    IReadOnlyList<PecaItem> Pecas
);
