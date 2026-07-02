using MediatR;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.AdicionarPecaInsumo;

public sealed class AdicionarPecaInsumoHandler : IRequestHandler<AdicionarPecaInsumoCommand, Result<AdicionarPecaInsumoResponse>>
{
    private readonly IPecaInsumoRepository _repository;

    public AdicionarPecaInsumoHandler(
        IPecaInsumoRepository repository
    ) => _repository = repository;

    public async Task<Result<AdicionarPecaInsumoResponse>> Handle(AdicionarPecaInsumoCommand command, CancellationToken cancellationToken)
    {
        if (await _repository.ExistePorNome(command.Nome, cancellationToken))
            return PecaInsumoErrors.NomeJaExiste;

        UnidadeDeMedida unidadeDeMedida = Enum.Parse<UnidadeDeMedida>(command.UnidadeDeMedida);
        Result<PecaInsumo> pecaInsumoResult = PecaInsumo.Criar(
            nome: command.Nome,
            descricao: command.Descricao,
            preco: command.Preco,
            quantidadeEmEstoque: command.QuantidadeEmEstoque,
            unidadeDeMedida: unidadeDeMedida
        );
        if (pecaInsumoResult.IsFailure)
            return pecaInsumoResult.Error;

        PecaInsumo pecaInsumo = pecaInsumoResult.Value;

        await _repository.Adicionar(pecaInsumo, cancellationToken);

        return AdicionarPecaInsumoResponse.FromPecaInsumo(pecaInsumo);
    }
}

