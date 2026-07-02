using OrdensServico.Domain.OrdemServico;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.IniciarDiagnostico;

public sealed record IniciarDiagnosticoCommand(Guid OrdemServicoId) : ICommand<OrdemServico>;
