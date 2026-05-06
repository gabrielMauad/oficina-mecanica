using Cadastro.Domain.Servico;
using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Servicos.Commands.AtualizarServico;

public sealed class AtualizarServicoHandler : IRequestHandler<AtualizarServicoCommand, Result<AtualizarServicoResponse>>
{
    private readonly IServicoRepository _repository;

    public AtualizarServicoHandler(IServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<AtualizarServicoResponse>> Handle(AtualizarServicoCommand command, CancellationToken cancellationToken)
    {
        ServicoId servicoId = new(command.ServicoId);
        Servico? servico = await _repository.ObterPorId(servicoId, cancellationToken);

        if (servico is null)
            return ServicoErrors.NaoEncontrado;

        if (!servico.Ativo)
            return ServicoErrors.JaDesativado;

        bool houveAlteracao = false;

        if (command.Preco is not null && command.Preco != servico.PrecoBase.Valor)
        {
            var result = servico.AtualizarPrecoBase(command.Preco!.Value);
            if (result.IsFailure) return result.Error;
            houveAlteracao = true;
        }

        if (command.Descricao is not null && command.Descricao != servico.Descricao)
        {
            var result = servico.AtualizarDescricao(command.Descricao!);
            if (result.IsFailure) return result.Error;
            houveAlteracao = true;
        }

        if (houveAlteracao)
            await _repository.Atualizar(servico, cancellationToken);

        return AtualizarServicoResponse.FromServico(servico);
    }
}

