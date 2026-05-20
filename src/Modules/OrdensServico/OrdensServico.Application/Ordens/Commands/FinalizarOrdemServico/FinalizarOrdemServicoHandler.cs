using MediatR;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.FinalizarOrdemServico;

public sealed class FinalizarOrdemServicoHandler : IRequestHandler<FinalizarOrdemServicoCommand, Result<OrdemServicoResumoDto>>
{
    private readonly IOrdemServicoRepository _ordemServicoRepository;
    public FinalizarOrdemServicoHandler(IOrdemServicoRepository ordemServicoRepository) =>
    _ordemServicoRepository = ordemServicoRepository;

    public async Task<Result<OrdemServicoResumoDto>> Handle(FinalizarOrdemServicoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _ordemServicoRepository.ObterPorId(ordemServicoId, ct);

        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        Result<OrdemServico> result = ordemServico.Finalizar();
        if (result.IsFailure)
            return result.Error;

        OrdemServico os = result.Value;
        await _ordemServicoRepository.Atualizar(os, ct);

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
