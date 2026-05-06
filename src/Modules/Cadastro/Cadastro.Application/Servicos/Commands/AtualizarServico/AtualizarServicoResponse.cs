using Cadastro.Domain.Servico;

namespace Cadastro.Application.Servicos.Commands.AtualizarServico;

public sealed record AtualizarServicoResponse(
    Guid ServicoId,
    string? Descricao,
    decimal Preco,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static AtualizarServicoResponse FromServico(Servico servico)
    {
        return new AtualizarServicoResponse(
            servico.Id.Value,
            servico.Descricao,
            servico.PrecoBase.Valor,
            servico.Ativo,
            servico.CadastradoEm,
            servico.AtualizadoEm
        );
    }
}

