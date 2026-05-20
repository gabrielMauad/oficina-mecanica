using OrdensServico.Contracts.Dtos;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.AprovarOrcamento;

public sealed record AprovarOrcamentoCommand(Guid OrdemServicoId) : ICommand<OrdemServicoResumoDto>;
