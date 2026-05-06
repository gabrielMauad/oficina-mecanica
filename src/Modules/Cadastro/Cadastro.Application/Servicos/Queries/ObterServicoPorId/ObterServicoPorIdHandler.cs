using Cadastro.Domain.Servico;
using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Servicos.Queries.ObterServicoPorId;

public sealed class ObterServicoPorIdHandler : IRequestHandler<ObterServicoPorIdQuery, Result<ObterServicoPorIdResponse>>
{
    private readonly IServicoRepository _repository;

    public ObterServicoPorIdHandler(IServicoRepository repository) => _repository = repository;

    public async Task<Result<ObterServicoPorIdResponse>> Handle(ObterServicoPorIdQuery request, CancellationToken cancellationToken)
    {
        ServicoId servicoId = new(request.ServicoId);
        Servico? servico = await _repository.ObterPorId(servicoId, cancellationToken);
        if (servico is null)
            return ServicoErrors.NaoEncontrado;

        return ObterServicoPorIdResponse.FromServico(servico);
    }
}

