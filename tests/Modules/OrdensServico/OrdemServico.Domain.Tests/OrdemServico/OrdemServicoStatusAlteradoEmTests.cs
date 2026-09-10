using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Domain.Tests.OrdemServico;

/// <summary>
/// Cobre o contrato de <see cref="Domain.OrdemServico.OrdemServico.StatusAlteradoEm"/>: o campo deve
/// avançar exclusivamente nos métodos que mudam <see cref="Domain.OrdemServico.OrdemServico.Status"/>,
/// e permanecer intocado em todos os demais — mesmo quando esses outros métodos mexem em
/// <see cref="Domain.OrdemServico.OrdemServico.AtualizadoEm"/>. É esse comportamento que faz a métrica
/// de duração por etapa (ver OrdensServicoMetrics) medir o intervalo correto.
/// </summary>
public class OrdemServicoStatusAlteradoEmTests
{
    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ServicoId = Guid.NewGuid();
    private static readonly Guid PecaId = Guid.NewGuid();

    private static Domain.OrdemServico.OrdemServico CriarOs() =>
        Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId).Value;

    private static Domain.OrdemServico.OrdemServico CriarOsEmDiagnostico()
    {
        var os = CriarOs();
        os.IniciarDiagnostico();
        return os;
    }

    private static Domain.OrdemServico.OrdemServico CriarOsAguardandoAprovacao()
    {
        var os = CriarOsEmDiagnostico();
        os.RegistrarDiagnostico("desc", [new ItemServicoInput(ServicoId, 1, 100m)], [new ItemPecaInput(PecaId, 1, 50m)]);
        os.EnviarOrcamento(DateTime.UtcNow);
        return os;
    }

    private static Domain.OrdemServico.OrdemServico CriarOsComOrcamentoAprovado()
    {
        var os = CriarOsAguardandoAprovacao();
        os.AprovarOrcamento();
        return os;
    }

    private static Domain.OrdemServico.OrdemServico CriarOsEmExecucao()
    {
        var os = CriarOsComOrcamentoAprovado();
        os.Executar();
        return os;
    }

    private static Domain.OrdemServico.OrdemServico CriarOsFinalizada()
    {
        var os = CriarOsEmExecucao();
        os.Finalizar();
        return os;
    }

    // ═══════════════════════════════════════════════
    // StatusAlteradoEm AVANÇA nos métodos que mudam Status
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "Criar: StatusAlteradoEm é preenchido na criação (OS nasce Recebida)")]
    public void Criar_ComDadosValidos_PreencheStatusAlteradoEm()
    {
        var os = CriarOs();

        Assert.NotEqual(default, os.StatusAlteradoEm);
    }

    [Fact(DisplayName = "AbrirComServicos: OS nasce direto em AguardandoAprovacao com StatusAlteradoEm preenchido")]
    public void AbrirComServicos_ComDadosValidos_PreencheStatusAlteradoEm()
    {
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };

        var os = Domain.OrdemServico.OrdemServico.AbrirComServicos(ClienteId, VeiculoId, servicos, pecas).Value;

        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.NotEqual(default, os.StatusAlteradoEm);
    }

    [Fact(DisplayName = "IniciarDiagnostico: Status muda → StatusAlteradoEm avança")]
    public void IniciarDiagnostico_QuandoRecebida_AvancaStatusAlteradoEm()
    {
        var os = CriarOs();
        var antes = os.StatusAlteradoEm;
        Thread.Sleep(5);

        var result = os.IniciarDiagnostico();

        Assert.True(result.IsSuccess);
        Assert.True(os.StatusAlteradoEm > antes);
    }

    [Fact(DisplayName = "EnviarOrcamento: Status muda → StatusAlteradoEm avança")]
    public void EnviarOrcamento_ComOrcamentoPendente_AvancaStatusAlteradoEm()
    {
        var os = CriarOsEmDiagnostico();
        os.RegistrarDiagnostico("desc", [new ItemServicoInput(ServicoId, 1, 100m)], [new ItemPecaInput(PecaId, 1, 50m)]);
        var antes = os.StatusAlteradoEm;
        Thread.Sleep(5);

        var result = os.EnviarOrcamento(DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.True(os.StatusAlteradoEm > antes);
    }

    [Fact(DisplayName = "Executar: Status muda → StatusAlteradoEm avança")]
    public void Executar_ComOrcamentoAprovado_AvancaStatusAlteradoEm()
    {
        var os = CriarOsComOrcamentoAprovado();
        var antes = os.StatusAlteradoEm;
        Thread.Sleep(5);

        var result = os.Executar();

        Assert.True(result.IsSuccess);
        Assert.True(os.StatusAlteradoEm > antes);
    }

    [Fact(DisplayName = "Finalizar: Status muda → StatusAlteradoEm avança")]
    public void Finalizar_QuandoEmExecucao_AvancaStatusAlteradoEm()
    {
        var os = CriarOsEmExecucao();
        var antes = os.StatusAlteradoEm;
        Thread.Sleep(5);

        var result = os.Finalizar();

        Assert.True(result.IsSuccess);
        Assert.True(os.StatusAlteradoEm > antes);
    }

    [Fact(DisplayName = "Concluir: Status muda → StatusAlteradoEm avança")]
    public void Concluir_QuandoFinalizadaENotificada_AvancaStatusAlteradoEm()
    {
        var os = CriarOsFinalizada();
        os.NotificarCliente(DateTime.UtcNow);
        var antes = os.StatusAlteradoEm;
        Thread.Sleep(5);

        var result = os.Concluir(DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.True(os.StatusAlteradoEm > antes);
    }

    // ═══════════════════════════════════════════════
    // StatusAlteradoEm NÃO SE MOVE nos métodos que não mudam Status
    // (estes são os testes que impedem a métrica de voltar a ficar viesada em silêncio)
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "RegistrarDiagnostico: Status não muda → StatusAlteradoEm permanece igual (mesmo alterando AtualizadoEm)")]
    public void RegistrarDiagnostico_QuandoEmDiagnostico_NaoAlteraStatusAlteradoEm()
    {
        var os = CriarOsEmDiagnostico();
        var antesStatusAlteradoEm = os.StatusAlteradoEm;
        var antesAtualizadoEm = os.AtualizadoEm;
        Thread.Sleep(5);

        var result = os.RegistrarDiagnostico("desc", [new ItemServicoInput(ServicoId, 1, 100m)], [new ItemPecaInput(PecaId, 1, 50m)]);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
        Assert.Equal(antesStatusAlteradoEm, os.StatusAlteradoEm);
        Assert.True(os.AtualizadoEm > antesAtualizadoEm); // confirma que o cenário realmente exercita a sobrescrita de AtualizadoEm
    }

    [Fact(DisplayName = "RegistrarDiagnostico após rejeição de orçamento: Status não muda (permanece AguardandoAprovacao) → StatusAlteradoEm permanece igual")]
    public void RegistrarDiagnostico_ApósRejeicaoDeOrcamento_NaoAlteraStatusAlteradoEm()
    {
        var os = CriarOsAguardandoAprovacao();
        os.RejeitarOrcamento();
        var antesStatusAlteradoEm = os.StatusAlteradoEm;
        Thread.Sleep(5);

        var result = os.RegistrarDiagnostico("novo diagnóstico", [new ItemServicoInput(ServicoId, 1, 100m)], [new ItemPecaInput(PecaId, 1, 50m)]);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(antesStatusAlteradoEm, os.StatusAlteradoEm);
    }

    [Fact(DisplayName = "AprovarOrcamento: Status não muda (permanece AguardandoAprovacao) → StatusAlteradoEm permanece igual")]
    public void AprovarOrcamento_ComOrcamentoEnviado_NaoAlteraStatusAlteradoEm()
    {
        var os = CriarOsAguardandoAprovacao();
        var antesStatusAlteradoEm = os.StatusAlteradoEm;
        var antesAtualizadoEm = os.AtualizadoEm;
        Thread.Sleep(5);

        var result = os.AprovarOrcamento();

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(antesStatusAlteradoEm, os.StatusAlteradoEm);
        Assert.True(os.AtualizadoEm > antesAtualizadoEm);
    }

    [Fact(DisplayName = "RejeitarOrcamento: Status não muda (permanece AguardandoAprovacao) → StatusAlteradoEm permanece igual")]
    public void RejeitarOrcamento_ComOrcamentoEnviado_NaoAlteraStatusAlteradoEm()
    {
        var os = CriarOsAguardandoAprovacao();
        var antesStatusAlteradoEm = os.StatusAlteradoEm;
        var antesAtualizadoEm = os.AtualizadoEm;
        Thread.Sleep(5);

        var result = os.RejeitarOrcamento();

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(antesStatusAlteradoEm, os.StatusAlteradoEm);
        Assert.True(os.AtualizadoEm > antesAtualizadoEm);
    }

    [Fact(DisplayName = "NotificarCliente: Status não muda (permanece Finalizada) → StatusAlteradoEm permanece igual")]
    public void NotificarCliente_QuandoFinalizada_NaoAlteraStatusAlteradoEm()
    {
        var os = CriarOsFinalizada();
        var antesStatusAlteradoEm = os.StatusAlteradoEm;
        var antesAtualizadoEm = os.AtualizadoEm;
        Thread.Sleep(5);

        var result = os.NotificarCliente(DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.Finalizada, os.Status);
        Assert.Equal(antesStatusAlteradoEm, os.StatusAlteradoEm);
        Assert.True(os.AtualizadoEm > antesAtualizadoEm);
    }

    // ═══════════════════════════════════════════════
    // Reconstituir: o timestamp persistido é o que volta, não um novo UtcNow
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "Reconstituir: StatusAlteradoEm é restaurado exatamente como persistido")]
    public void Reconstituir_ComStatusAlteradoEmPersistido_RestauraValorExato()
    {
        var statusAlteradoEmPersistido = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        var os = Domain.OrdemServico.OrdemServico.Reconstituir(
            OrdemServicoId.Novo(),
            ClienteId,
            VeiculoId,
            StatusOrdemServico.EmExecucao,
            descricaoDiagnostico: "desc",
            notificadoEm: null,
            entregueEm: null,
            criadoEm: new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
            atualizadoEm: new DateTime(2026, 1, 16, 9, 0, 0, DateTimeKind.Utc),
            statusAlteradoEm: statusAlteradoEmPersistido,
            itensServico: [],
            itensPeca: [],
            orcamentos: []);

        Assert.Equal(statusAlteradoEmPersistido, os.StatusAlteradoEm);
    }
}
