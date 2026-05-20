using OrdensServico.Contracts.Dtos;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.ConcluirOrdemServico;

public sealed record ConcluirOrdemServicoCommand(Guid OrdemServicoId) : ICommand<OrdemServicoResumoDto>;
