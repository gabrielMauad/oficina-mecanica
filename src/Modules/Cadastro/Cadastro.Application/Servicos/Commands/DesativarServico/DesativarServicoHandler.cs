using Cadastro.Domain.Servico;
using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Servicos.Commands.DesativarServico;

public sealed class DesativarServicoHandler : IRequestHandler<DesativarServicoCommand, Result<DesativarServicoResponse>>
{
    private readonly IServicoRepository _repository;

    public DesativarServicoHandler(IServicoRepository repository) => _repository = repository;

    public async Task<Result<DesativarServicoResponse>> Handle(DesativarServicoCommand command, CancellationToken cancellationToken)
    {
        ServicoId servicoId = new(command.ServicoId);
        Servico? servico = await _repository.ObterPorId(servicoId, cancellationToken);
        if (servico is null)
            return ServicoErrors.NaoEncontrado;
        if (!servico.Ativo)
            return ServicoErrors.JaDesativado;
        servico.Desativar();
        await _repository.Atualizar(servico, cancellationToken);

        return DesativarServicoResponse.FromServico(servico);
    }
}

