using MediatR;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Domain.OrdemServico;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Commands.IniciarDiagnostico;

public sealed class IniciarDiagnosticoHandler : IRequestHandler<IniciarDiagnosticoCommand, Result<OrdemServicoResumoDto>>
{
    private readonly IOrdemServicoRepository _repository;

    public IniciarDiagnosticoHandler(
        IOrdemServicoRepository repository
    ) => _repository = repository;

    public async Task<Result<OrdemServicoResumoDto>> Handle(IniciarDiagnosticoCommand command, CancellationToken ct)
    {
        OrdemServicoId ordemServicoId = new(command.OrdemServicoId);
        OrdemServico? ordemServico = await _repository.ObterPorId(ordemServicoId, ct);
        if (ordemServico is null)
            return OrdemServicoErrors.NaoEncontrada;

        Result<OrdemServico> resultado = ordemServico.IniciarDiagnostico();
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
           [],
           [],
           []
       );
    }
}
