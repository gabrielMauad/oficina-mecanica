using OrdensServico.Application.Ordens.Commands.RegistrarDiagnostico;

namespace OrdensServico.Adapters.Models.Request;

public sealed record AbrirOrdemServicoCompletaRequest(
    Guid ClienteId,
    Guid VeiculoId,
    IReadOnlyList<ServicoItem> Servicos,
    IReadOnlyList<PecaItem> Pecas
);
