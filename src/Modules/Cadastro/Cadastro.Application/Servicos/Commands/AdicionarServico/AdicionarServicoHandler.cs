using Cadastro.Domain.Servico;
using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Servicos.Commands.AdicionarServico;

public sealed class AdicionarServicoHandler : IRequestHandler<AdicionarServicoCommand, Result<AdicionarServicoResponse>>
{
    private readonly IServicoRepository _repository;

    public AdicionarServicoHandler(
        IServicoRepository repository
    ) => _repository = repository;

    public async Task<Result<AdicionarServicoResponse>> Handle(AdicionarServicoCommand command, CancellationToken cancellationToken)
    {
        if (await _repository.ExistePorNome(command.Nome, cancellationToken))
            return ServicoErrors.NomeJaExiste;

        Result<Servico> servicoResult = Servico.Criar(command.Nome, command.Descricao, command.Preco);
        if (servicoResult.IsFailure)
            return servicoResult.Error;

        Servico servico = servicoResult.Value;

        await _repository.Adicionar(servico, cancellationToken);

        return AdicionarServicoResponse.FromServico(servico);
    }
}

