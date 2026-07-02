using MediatR;
using OrdensServico.Application.Gateways;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.RejeitarOrcamento;

public sealed class RejeitarOrcamentoHandler : IRequestHandler<RejeitarOrcamentoCommand, Result<OrdemServicoResumoDto>>
{
    private readonly IOrdemServicoGateway _ordemServicoGateway;

    public RejeitarOrcamentoHandler(IOrdemServicoGateway ordemServicoGateway) =>
        _ordemServicoGateway = ordemServicoGateway;

    public async Task<Result<OrdemServicoResumoDto>> Handle(RejeitarOrcamentoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _ordemServicoGateway.ObterPorId(ordemServicoId, ct);

        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        Result<OrdemServico> result = ordemServico.RejeitarOrcamento();
        if (result.IsFailure)
            return result.Error;

        OrdemServico os = result.Value;
        await _ordemServicoGateway.Atualizar(os, ct);

        return new OrdemServicoResumoDto(
            os.Id.Value,
            os.ClienteId,
            os.VeiculoId,
            os.Status.ToString(),
            os.DescricaoDiagnostico,
            os.NotificadoEm,
            os.EntregueEm,
            os.CriadoEm,
            os.AtualizadoEm,
            [.. os.ItensServico.Select(x => new ItemServicoDto(x.ServicoId, x.Quantidade, x.PrecoUnitarioSnapshot))],
            [.. os.ItensPeca.Select(x => new ItemPecaDto(x.PecaInsumoId, x.Quantidade, x.PrecoUnitarioSnapshot))],
            [.. os.Orcamentos.Select(x => new OrcamentoDto(x.ValorTotal, x.Status.ToString(), x.DataGeracao, x.DataEnvio, x.DataAprovacao))]
        );
    }
}
