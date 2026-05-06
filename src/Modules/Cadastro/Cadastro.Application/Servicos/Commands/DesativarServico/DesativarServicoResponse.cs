using Cadastro.Domain.Servico;

namespace Cadastro.Application.Servicos.Commands.DesativarServico;

public sealed record DesativarServicoResponse(
    Guid ServicoId,
    string Nome,
    bool Ativo
)
{
    public static DesativarServicoResponse FromServico(Servico servico)
    {
        return new DesativarServicoResponse(
            servico.Id.Value,
            servico.Nome,
            servico.Ativo
        );
    }
}