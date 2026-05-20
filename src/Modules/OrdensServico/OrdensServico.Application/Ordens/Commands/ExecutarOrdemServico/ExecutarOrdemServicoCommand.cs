using OrdensServico.Contracts.Dtos;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.ExecutarOrdemServico;

public sealed record ExecutarOrdemServicoCommand(Guid OrdemServicoId) : ICommand<OrdemServicoResumoDto>;
