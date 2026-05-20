using OrdensServico.Contracts.Dtos;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.RegistrarDiagnostico;

public sealed record RegistrarDiagnosticoCommand(
    Guid OrdemServicoId,
    string DescricaoDiagnostico,
    IReadOnlyList<ServicoItem> Servicos,
    IReadOnlyList<PecaItem> Pecas
) : ICommand<OrdemServicoResumoDto>;

public sealed record ServicoItem(Guid ServicoId, int Quantidade);
public sealed record PecaItem(Guid PecaInsumoId, int Quantidade);
