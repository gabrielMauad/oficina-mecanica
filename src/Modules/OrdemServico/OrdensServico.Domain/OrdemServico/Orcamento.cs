using SharedKernel.Domain;

namespace OrdensServico.Domain.OrdemServico;

public sealed class Orcamento : Entity<OrcamentoId>
{
    public decimal ValorTotal { get; private set; }
    public StatusOrcamento Status { get; private set; }
    public DateTime DataGeracao { get; private set; }
    public DateTime? DataEnvio { get; private set; }
    public DateTime? DataAprovacao { get; private set; }

    private Orcamento(OrcamentoId id, decimal valorTotal) : base(id)
    {
        ValorTotal = valorTotal;
        Status = StatusOrcamento.Pendente;
        DataGeracao = DateTime.UtcNow;
    }

    internal static Orcamento Criar(decimal valorTotal)
        => new(OrcamentoId.Novo(), valorTotal);

    internal Result<Orcamento> Enviar(DateTime dataEnvio)
    {
        if (Status != StatusOrcamento.Pendente)
            return Error.Validation("Orcamento.TransicaoInvalida", "OrÃ§amento sÃ³ pode ser enviado quando estÃ¡ Pendente.");

        Status = StatusOrcamento.Enviado;
        DataEnvio = dataEnvio;
        return this;
    }

    internal Result<Orcamento> Aprovar()
    {
        if (Status != StatusOrcamento.Enviado)
            return Error.Validation("Orcamento.TransicaoInvalida", "OrÃ§amento sÃ³ pode ser aprovado quando estÃ¡ Enviado.");

        Status = StatusOrcamento.Aprovado;
        DataAprovacao = DateTime.UtcNow;
        return this;
    }

    internal Result<Orcamento> Rejeitar()
    {
        if (Status != StatusOrcamento.Enviado)
            return Error.Validation("Orcamento.TransicaoInvalida", "OrÃ§amento sÃ³ pode ser rejeitado quando estÃ¡ Enviado.");

        Status = StatusOrcamento.Rejeitado;
        return this;
    }
}
