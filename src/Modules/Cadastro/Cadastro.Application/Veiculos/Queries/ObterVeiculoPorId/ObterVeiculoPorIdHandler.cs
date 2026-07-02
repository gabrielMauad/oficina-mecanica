using Cadastro.Domain.Veiculo;
using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Veiculos.Queries.ObterVeiculoPorId;

public sealed class ObterVeiculoPorIdHandler
    : IRequestHandler<ObterVeiculoPorIdQuery, Result<ObterVeiculoPorIdResponse>>
{
    private readonly IVeiculoRepository _repository;

    public ObterVeiculoPorIdHandler(IVeiculoRepository repository) => _repository = repository;

    public async Task<Result<ObterVeiculoPorIdResponse>> Handle(ObterVeiculoPorIdQuery request, CancellationToken cancellationToken)
    {
        VeiculoId veiculoId = new(request.VeiculoId);
        var veiculo = await _repository.ObterPorId(veiculoId, cancellationToken);
        if (veiculo is null)
            return VeiculoErrors.NaoEncontrado;
        return ObterVeiculoPorIdResponse.FromVeiculo(veiculo);
    }

}

