using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Domain.Tests.OrdemServico;

public class OrdemServicoTransicoesTests
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
    // Criar
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "Criar: dados válidos → OS com status Recebida e IDs preenchidos")]
    public void Criar_ComDadosValidos_RetornaOrdemServicoComStatusRecebida()
    {
        var result = Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.Recebida, result.Value.Status);
        Assert.Equal(ClienteId, result.Value.ClienteId);
        Assert.Equal(VeiculoId, result.Value.VeiculoId);
        Assert.NotEqual(Guid.Empty, result.Value.Id.Value);
        Assert.Empty(result.Value.ItensServico);
        Assert.Empty(result.Value.ItensPeca);
        Assert.Empty(result.Value.Orcamentos);
    }

    [Fact(DisplayName = "Criar: ClienteId vazio → erro ClienteIdVazio")]
    public void Criar_ComClienteIdVazio_RetornaErroClienteIdVazio()
    {
        var result = Domain.OrdemServico.OrdemServico.Criar(Guid.Empty, VeiculoId);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.ClienteIdVazio", result.Error.Code);
    }

    [Fact(DisplayName = "Criar: VeiculoId vazio → erro VeiculoIdVazio")]
    public void Criar_ComVeiculoIdVazio_RetornaErroVeiculoIdVazio()
    {
        var result = Domain.OrdemServico.OrdemServico.Criar(ClienteId, Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.VeiculoIdVazio", result.Error.Code);
    }

    // ═══════════════════════════════════════════════
    // IniciarDiagnostico
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "IniciarDiagnostico: Recebida → EmDiagnostico")]
    public void IniciarDiagnostico_QuandoRecebida_TransitaParaEmDiagnostico()
    {
        var os = CriarOs();

        var result = os.IniciarDiagnostico();

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
    }

    [Fact(DisplayName = "IniciarDiagnostico: status diferente de Recebida → TransicaoInvalida")]
    public void IniciarDiagnostico_QuandoNaoRecebida_RetornaErroTransicaoInvalida()
    {
        var os = CriarOsEmDiagnostico(); // já está em EmDiagnostico

        var result = os.IniciarDiagnostico();

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    // ═══════════════════════════════════════════════
    // EnviarOrcamento
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "EnviarOrcamento: EmDiagnostico+Pendente → AguardandoAprovacao, DataEnvio preenchida")]
    public void EnviarOrcamento_ComOrcamentoPendente_TransitaParaAguardandoAprovacao()
    {
        var os = CriarOsEmDiagnostico();
        os.RegistrarDiagnostico("desc", [new ItemServicoInput(ServicoId, 1, 100m)], [new ItemPecaInput(PecaId, 1, 50m)]);
        var dataEnvio = DateTime.UtcNow;

        var result = os.EnviarOrcamento(dataEnvio);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(StatusOrcamento.Enviado, os.Orcamentos[0].Status);
        Assert.NotNull(os.Orcamentos[0].DataEnvio);
    }

    [Fact(DisplayName = "EnviarOrcamento: sem orçamento pendente → OrcamentoNaoEncontrado")]
    public void EnviarOrcamento_SemOrcamentoPendente_RetornaErroOrcamentoNaoEncontrado()
    {
        var os = CriarOsAguardandoAprovacao(); // orcamento já está Enviado, não Pendente

        var result = os.EnviarOrcamento(DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.OrcamentoNaoEncontrado", result.Error.Code);
    }

    // ═══════════════════════════════════════════════
    // AprovarOrcamento
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "AprovarOrcamento: AguardandoAprovacao+Enviado → orçamento Aprovado, OS permanece AguardandoAprovacao")]
    public void AprovarOrcamento_ComOrcamentoEnviado_AlteraStatusDoOrcamentoParaAprovado()
    {
        var os = CriarOsAguardandoAprovacao();

        var result = os.AprovarOrcamento();

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamentos[0].Status);
    }

    [Fact(DisplayName = "AprovarOrcamento: status Recebida → TransicaoInvalida")]
    public void AprovarOrcamento_QuandoNaoAguardandoAprovacao_RetornaErroTransicaoInvalida()
    {
        var os = CriarOs(); // Recebida

        var result = os.AprovarOrcamento();

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    [Fact(DisplayName = "AprovarOrcamento: AguardandoAprovacao sem orçamento Enviado → OrcamentoNaoEncontrado")]
    public void AprovarOrcamento_SemOrcamentoEnviado_RetornaErroOrcamentoNaoEncontrado()
    {
        var os = CriarOsAguardandoAprovacao();
        os.RejeitarOrcamento(); // orcamento agora é Rejeitado; OS continua AguardandoAprovacao

        var result = os.AprovarOrcamento();

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.OrcamentoNaoEncontrado", result.Error.Code);
    }

    // ═══════════════════════════════════════════════
    // RejeitarOrcamento
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "RejeitarOrcamento: status Recebida → TransicaoInvalida")]
    public void RejeitarOrcamento_QuandoNaoAguardandoAprovacao_RetornaErroTransicaoInvalida()
    {
        var os = CriarOs(); // Recebida

        var result = os.RejeitarOrcamento();

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    [Fact(DisplayName = "RejeitarOrcamento: AguardandoAprovacao sem orçamento Enviado → OrcamentoNaoEncontrado")]
    public void RejeitarOrcamento_SemOrcamentoEnviado_RetornaErroOrcamentoNaoEncontrado()
    {
        var os = CriarOsAguardandoAprovacao();
        os.RejeitarOrcamento(); // orcamento agora Rejeitado; OS continua AguardandoAprovacao

        var result = os.RejeitarOrcamento(); // nenhum Enviado para rejeitar

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.OrcamentoNaoEncontrado", result.Error.Code);
    }

    // ═══════════════════════════════════════════════
    // Executar
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "Executar: AguardandoAprovacao+Aprovado → EmExecucao")]
    public void Executar_ComOrcamentoAprovado_TransitaParaEmExecucao()
    {
        var os = CriarOsComOrcamentoAprovado();

        var result = os.Executar();

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
    }

    [Fact(DisplayName = "Executar: status Recebida → TransicaoInvalida")]
    public void Executar_QuandoNaoAguardandoAprovacao_RetornaErroTransicaoInvalida()
    {
        var os = CriarOs(); // Recebida

        var result = os.Executar();

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    [Fact(DisplayName = "Executar: AguardandoAprovacao sem orçamento Aprovado → TransicaoInvalida")]
    public void Executar_SemOrcamentoAprovado_RetornaErroTransicaoInvalida()
    {
        var os = CriarOsAguardandoAprovacao(); // orçamento está Enviado, não Aprovado

        var result = os.Executar();

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    // ═══════════════════════════════════════════════
    // Finalizar
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "Finalizar: status diferente de EmExecucao → TransicaoInvalida")]
    public void Finalizar_QuandoNaoEmExecucao_RetornaErroTransicaoInvalida()
    {
        var os = CriarOs(); // Recebida

        var result = os.Finalizar();

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    // ═══════════════════════════════════════════════
    // NotificarCliente
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "NotificarCliente: Finalizada → NotificadoEm preenchido")]
    public void NotificarCliente_QuandoFinalizada_PreencheNotificadoEm()
    {
        var os = CriarOsFinalizada();
        var dataNotificacao = DateTime.UtcNow;

        var result = os.NotificarCliente(dataNotificacao);

        Assert.True(result.IsSuccess);
        Assert.Equal(dataNotificacao, os.NotificadoEm);
        Assert.Equal(StatusOrdemServico.Finalizada, os.Status); // status não muda
    }

    [Fact(DisplayName = "NotificarCliente: status diferente de Finalizada → TransicaoInvalida")]
    public void NotificarCliente_QuandoNaoFinalizada_RetornaErroTransicaoInvalida()
    {
        var os = CriarOs(); // Recebida

        var result = os.NotificarCliente(DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    // ═══════════════════════════════════════════════
    // Concluir
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "Concluir: Finalizada+notificado → Entregue, EntregueEm preenchido")]
    public void Concluir_QuandoFinalizadaENotificada_TransitaParaEntregue()
    {
        var os = CriarOsFinalizada();
        os.NotificarCliente(DateTime.UtcNow);
        var dataEntrega = DateTime.UtcNow;

        var result = os.Concluir(dataEntrega);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.Entregue, os.Status);
        Assert.Equal(dataEntrega, os.EntregueEm);
    }

    [Fact(DisplayName = "Concluir: status diferente de Finalizada → TransicaoInvalida")]
    public void Concluir_QuandoNaoFinalizada_RetornaErroTransicaoInvalida()
    {
        var os = CriarOs(); // Recebida

        var result = os.Concluir(DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    [Fact(DisplayName = "Concluir: Finalizada sem NotificarCliente → TransicaoInvalida")]
    public void Concluir_QuandoFinalizadaMasNaoNotificada_RetornaErroTransicaoInvalida()
    {
        var os = CriarOsFinalizada(); // Finalizada, mas NotificadoEm == null

        var result = os.Concluir(DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    // ═══════════════════════════════════════════════
    // Snapshot de preço e cálculo de ValorTotal
    // ═══════════════════════════════════════════════

    [Fact(DisplayName = "Snapshot: PrecoUnitarioSnapshot dos itens reflete o preço informado na criação")]
    public void RegistrarDiagnostico_ItensGravamPrecoUnitarioSnapshot()
    {
        var os = CriarOsEmDiagnostico();
        var precoServico = 150m;
        var precoPeca = 75m;

        os.RegistrarDiagnostico(
            "desc",
            [new ItemServicoInput(ServicoId, 2, precoServico)],
            [new ItemPecaInput(PecaId, 3, precoPeca)]);

        Assert.Equal(precoServico, os.ItensServico[0].PrecoUnitarioSnapshot);
        Assert.Equal(precoPeca, os.ItensPeca[0].PrecoUnitarioSnapshot);
    }

    [Fact(DisplayName = "ValorTotal: calculado corretamente como soma de (qtd × preço) de serviços e peças")]
    public void RegistrarDiagnostico_ValorTotalCalculadoCorretamente()
    {
        var os = CriarOsEmDiagnostico();
        // 2 serviços a 100 = 200; 3 peças a 50 = 150; total = 350
        var servicos = new[]
        {
            new ItemServicoInput(ServicoId, 1, 100m),
            new ItemServicoInput(Guid.NewGuid(), 1, 100m)
        };
        var pecas = new[]
        {
            new ItemPecaInput(PecaId, 3, 50m)
        };

        os.RegistrarDiagnostico("desc", servicos, pecas);

        Assert.Equal(350m, os.Orcamentos[0].ValorTotal);
    }
}
