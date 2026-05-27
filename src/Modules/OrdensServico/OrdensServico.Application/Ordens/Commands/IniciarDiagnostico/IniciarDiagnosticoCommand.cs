using OrdensServico.Contracts.Dtos;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.IniciarDiagnostico;

public sealed record IniciarDiagnosticoCommand(Guid OrdemServicoId) : ICommand<OrdemServicoResumoDto>;
