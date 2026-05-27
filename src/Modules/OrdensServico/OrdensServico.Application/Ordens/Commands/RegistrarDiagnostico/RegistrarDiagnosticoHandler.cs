using MediatR;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using OrdensServico.Domain.Ports;
using OrdensServico.Domain.Ports.Dtos;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.RegistrarDiagnostico;

public sealed class RegistrarDiagnosticoHandler : IRequestHandler<RegistrarDiagnosticoCommand, Result<OrdemServicoResumoDto>>
{
    private readonly IServicoInfoPort _servicoInfoPort;
    private readonly IPecaDisponibilidadePort _pecaDisponibilidadePort;
    private readonly IOrdemServicoRepository _repository;

    public RegistrarDiagnosticoHandler(
        IServicoInfoPort servicoInfoPort,
        IPecaDisponibilidadePort pecaDisponibilidadePort,
        IOrdemServicoRepository repository
    )
    {
        _servicoInfoPort = servicoInfoPort;
        _pecaDisponibilidadePort = pecaDisponibilidadePort;
        _repository = repository;
    }

    public async Task<Result<OrdemServicoResumoDto>> Handle(RegistrarDiagnosticoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _repository.ObterPorId(ordemServicoId, ct);

        if (ordemServico == null)
            return OrdemServicoErrors.NaoEncontrada;

        Task<Result<List<ItemServicoInput>>> servicosTask = ObterServicosAsync(command.Servicos, ct);
        Task<Result<List<ItemPecaInput>>> pecasTask = ObterPecasAsync(command.Pecas, ct);

        await Task.WhenAll(servicosTask, pecasTask);

        Result<List<ItemServicoInput>> servicosResult = await servicosTask;
        if (servicosResult.IsFailure)
            return servicosResult.Error;

        Result<List<ItemPecaInput>> pecasResult = await pecasTask;
        if (pecasResult.IsFailure)
            return pecasResult.Error;

        List<ItemServicoInput> itemServicoList = servicosResult.Value;
        List<ItemPecaInput> itemPecaList = pecasResult.Value;

        Result<OrdemServico> resultado = ordemServico.RegistrarDiagnostico(command.DescricaoDiagnostico, itemServicoList, itemPecaList);
        if (resultado.IsFailure)
            return resultado.Error;

        OrdemServico os = resultado.Value;

        await _repository.Atualizar(os, ct);

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

    private async Task<Result<List<ItemServicoInput>>> ObterServicosAsync(
        IReadOnlyList<ServicoItem> servicos,
        CancellationToken ct)
    {
        List<ItemServicoInput> itemServicoList = [];
        foreach (var servico in servicos)
        {
            decimal? snapshotPreco = await _servicoInfoPort.ObterPreco(servico.ServicoId, ct);
            if (snapshotPreco == null)
                return OrdemServicoErrors.ServicoNaoEncontrado;
            itemServicoList.Add(new ItemServicoInput(servico.ServicoId, servico.Quantidade, snapshotPreco.Value));
        }

        return itemServicoList;
    }

    private async Task<Result<List<ItemPecaInput>>> ObterPecasAsync(
        IReadOnlyList<PecaItem> pecas,
        CancellationToken ct)
    {
        List<ItemPecaInput> itemPecaList = [];
        foreach (var peca in pecas)
        {
            PecaDisponibilidade? disponibilidade = await _pecaDisponibilidadePort.Verificar(peca.PecaInsumoId, peca.Quantidade, ct);
            if (disponibilidade == null)
                return OrdemServicoErrors.PecaNaoEncontrada;
            if (!disponibilidade.Disponivel)
                return OrdemServicoErrors.PecaIndisponivel;

            itemPecaList.Add(new ItemPecaInput(peca.PecaInsumoId, peca.Quantidade, disponibilidade.PrecoUnitario));
        }

        return itemPecaList;
    }
}
