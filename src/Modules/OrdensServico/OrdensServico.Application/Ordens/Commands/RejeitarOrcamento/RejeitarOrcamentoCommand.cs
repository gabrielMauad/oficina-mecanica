using OrdensServico.Domain.OrdemServico;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.RejeitarOrcamento;

public sealed record RejeitarOrcamentoCommand(Guid OrdemServicoId) : ICommand<OrdemServico>;
