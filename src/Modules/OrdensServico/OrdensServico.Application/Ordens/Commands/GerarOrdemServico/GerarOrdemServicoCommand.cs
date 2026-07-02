using OrdensServico.Domain.OrdemServico;
using SharedKernel.Application;

namespace OrdensServico.Application.Ordens.Commands.GerarOrdemServico;

public sealed record GerarOrdemServicoCommand(Guid ClienteId, Guid VeiculoId) : ICommand<OrdemServico>;
