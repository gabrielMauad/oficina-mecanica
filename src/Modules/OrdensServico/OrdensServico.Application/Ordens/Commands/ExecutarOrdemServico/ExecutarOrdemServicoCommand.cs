using OrdensServico.Domain.OrdemServico;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.ExecutarOrdemServico;

public sealed record ExecutarOrdemServicoCommand(Guid OrdemServicoId) : ICommand<OrdemServico>;
