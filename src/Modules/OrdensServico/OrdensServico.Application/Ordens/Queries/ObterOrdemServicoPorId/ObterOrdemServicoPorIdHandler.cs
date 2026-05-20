using MediatR;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ObterOrdemServicoPorId;

public sealed class ObterOrdemServicoPorIdHandler : IRequestHandler<ObterOrdemServicoPorIdQuery, Result<OrdemServicoResumoDto>>
{
    private readonly IOrdemServicoRepository _ordemServicoRepository;

    public ObterOrdemServicoPorIdHandler(IOrdemServicoRepository ordemServicoRepository) =>
        _ordemServicoRepository = ordemServicoRepository;

    public async Task<Result<OrdemServicoResumoDto>> Handle(ObterOrdemServicoPorIdQuery request, CancellationToken ct)
    {
        var ordemServico = await _ordemServicoRepository.ObterPorId(
            new OrdemServicoId(request.OrdemServicoId),
            ct
        );

        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        return new OrdemServicoResumoDto(
            ordemServico.Id.Value,
            ordemServico.ClienteId,
            ordemServico.VeiculoId,
            ordemServico.Status.ToString(),
            ordemServico.DescricaoDiagnostico,
            ordemServico.NotificadoEm,
            ordemServico.EntregueEm,
            ordemServico.CriadoEm,
            ordemServico.AtualizadoEm,
            [.. ordemServico.ItensServico.Select(x => new ItemServicoDto(x.ServicoId, x.Quantidade, x.PrecoUnitarioSnapshot))],
            [.. ordemServico.ItensPeca.Select(x => new ItemPecaDto(x.PecaInsumoId, x.Quantidade, x.PrecoUnitarioSnapshot))],
            [.. ordemServico.Orcamentos.Select(x => new OrcamentoDto(x.ValorTotal, x.Status.ToString(), x.DataGeracao, x.DataEnvio, x.DataAprovacao))]
        );
    }
}
