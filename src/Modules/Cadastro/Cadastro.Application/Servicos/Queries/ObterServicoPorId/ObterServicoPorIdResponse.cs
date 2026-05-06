using Cadastro.Domain.Servico;

namespace Cadastro.Application.Servicos.Queries.ObterServicoPorId;

public sealed record ObterServicoPorIdResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Preco,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static ObterServicoPorIdResponse FromServico(Servico servico)
    {
        return new ObterServicoPorIdResponse(
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

