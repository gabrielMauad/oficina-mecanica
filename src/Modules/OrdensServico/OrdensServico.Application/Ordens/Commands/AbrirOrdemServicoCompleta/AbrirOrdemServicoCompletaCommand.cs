using OrdensServico.Application.Ordens.Commands.RegistrarDiagnostico;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.AbrirOrdemServicoCompleta;

public sealed record AbrirOrdemServicoCompletaCommand(
    Guid ClienteId,
    Guid VeiculoId,
    IReadOnlyList<ServicoItem> Servicos,
    IReadOnlyList<PecaItem> Pecas
) : ICommand<OrdemServico>;
