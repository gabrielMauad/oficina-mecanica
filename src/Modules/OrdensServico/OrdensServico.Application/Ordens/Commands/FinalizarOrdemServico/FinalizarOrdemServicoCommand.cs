using OrdensServico.Domain.OrdemServico;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.FinalizarOrdemServico;

public sealed record FinalizarOrdemServicoCommand(Guid OrdemServicoId) : ICommand<OrdemServico>;
