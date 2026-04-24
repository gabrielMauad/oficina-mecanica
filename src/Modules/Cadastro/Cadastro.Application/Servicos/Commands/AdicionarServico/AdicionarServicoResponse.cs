using Cadastro.Domain.Servico;

namespace Cadastro.Application.Servicos.Commands.AdicionarServico;

public sealed record AdicionarServicoResponse(
    Guid ServicoId,
    string Nome,
    string? Descricao,
    decimal PrecoBase,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static AdicionarServicoResponse FromServico(Servico servico)
    {
        return new AdicionarServicoResponse(
            servico.Id.Value,
            servico.Nome,
            servico.Descricao,
            servico.PrecoBase.Valor,
            servico.Ativo,
            servico.CadastradoEm,
            servico.AtualizadoEm
        );
    }
}

