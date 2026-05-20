using OrdensServico.Contracts.Dtos;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.RejeitarOrcamento;

public sealed record RejeitarOrcamentoCommand(Guid OrdemServicoId) : ICommand<OrdemServicoResumoDto>;
