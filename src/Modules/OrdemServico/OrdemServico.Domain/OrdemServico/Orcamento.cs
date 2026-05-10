using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico;

public sealed class Orcamento : Entity<OrcamentoId>
{
    public OrdemServicoId OrdemServicoId { get; private set; }
    public decimal ValorTotal { get; private set; }
    public StatusOrcamento Status { get; private set; }
    public DateTime DataGeracao { get; private set; }
    public DateTime? DataEnvio { get; private set; }
    public DateTime? DataAprovacao { get; private set; }

    private Orcamento(OrcamentoId id, OrdemServicoId ordemServicoId, decimal valorTotal) : base(id)
    {
        OrdemServicoId = ordemServicoId;
        ValorTotal = valorTotal;
        Status = StatusOrcamento.Pendente;
        DataGeracao = DateTime.UtcNow;
    }

    internal static Orcamento Criar(OrdemServicoId ordemServicoId, decimal valorTotal)
        => new(OrcamentoId.Novo(), ordemServicoId, valorTotal);

    internal Result<Orcamento> Enviar(DateTime dataEnvio)
    {
        if (Status != StatusOrcamento.Pendente)
            return Error.Validation("Orcamento.TransicaoInvalida", "Orçamento só pode ser enviado quando está Pendente.");

        Status = StatusOrcamento.Enviado;
        DataEnvio = dataEnvio;
        return this;
    }

    internal Result<Orcamento> Aprovar()
    {
        if (Status != StatusOrcamento.Enviado)
            return Error.Validation("Orcamento.TransicaoInvalida", "Orçamento só pode ser aprovado quando está Enviado.");

        Status = StatusOrcamento.Aprovado;
        DataAprovacao = DateTime.UtcNow;
        return this;
    }

    internal Result<Orcamento> Rejeitar()
    {
        if (Status != StatusOrcamento.Enviado)
            return Error.Validation("Orcamento.TransicaoInvalida", "Orçamento só pode ser rejeitado quando está Enviado.");

        Status = StatusOrcamento.Rejeitado;
        return this;
    }
}
