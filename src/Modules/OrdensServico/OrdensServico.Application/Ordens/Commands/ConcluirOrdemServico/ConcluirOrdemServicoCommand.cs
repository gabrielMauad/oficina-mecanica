using OrdensServico.Domain.OrdemServico;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.ConcluirOrdemServico;

public sealed record ConcluirOrdemServicoCommand(Guid OrdemServicoId) : ICommand<OrdemServico>;
