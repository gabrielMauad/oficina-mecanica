using OrdensServico.Domain.OrdemServico;
using OrdensServico.Domain.OrdemServico.Events;

namespace OrdensServico.Domain.Tests.OrdemServico;

public class OrdemServicoTests
{
    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ServicoId = Guid.NewGuid();
    private static readonly Guid PecaId = Guid.NewGuid();

    private static Domain.OrdemServico.OrdemServico CriarOsEmDiagnostico()
    {
        var os = Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId).Value;
        os.IniciarDiagnostico();
        return os;
    }

    private static Domain.OrdemServico.OrdemServico CriarOsAposRejeicao()
    {
        var os = CriarOsEmDiagnostico();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 2, 50m) };
        os.RegistrarDiagnostico("descriÃ§Ã£o original", servicos, pecas);
        os.EnviarOrcamento(DateTime.UtcNow);
        os.RejeitarOrcamento();
        os.ClearDomainEvents();
        return os;
    }


    [Fact]
    public void RegistrarDiagnostico_AposRejeicaoComTodosOrcamentosRejeitados_CriaNovoOrcamentoPendente()
    {
        var os = CriarOsAposRejeicao();
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 200m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 80m) };

        var result = os.RegistrarDiagnostico("Novo diagnÃ³stico", servicos, pecas);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(2, os.Orcamentos.Count);
        Assert.Equal(StatusOrcamento.Rejeitado, os.Orcamentos[0].Status);
        Assert.Equal(StatusOrcamento.Pendente, os.Orcamentos[1].Status);
        Assert.Single(os.DomainEvents.OfType<DiagnosticoConcluido>());
    }


    [Fact]
    public void RegistrarDiagnostico_ComOrcamentoPendenteExistente_RetornaErroOrcamentoExistente()
    {
        var os = CriarOsEmDiagnostico();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };
        os.RegistrarDiagnostico("primeiro", servicos, pecas);

        var result = os.RegistrarDiagnostico("segundo", servicos, pecas);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.OrcamentoExistente", result.Error.Code);
    }

    [Fact]
    public void RegistrarDiagnostico_QuandoAguardandoAprovacaoComOrcamentoEnviado_RetornaErroOrcamentoExistente()
    {
        var os = CriarOsEmDiagnostico();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };
        os.RegistrarDiagnostico("desc", servicos, pecas);
        os.EnviarOrcamento(DateTime.UtcNow);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);

        var result = os.RegistrarDiagnostico("segundo", servicos, pecas);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.OrcamentoExistente", result.Error.Code);
    }


    [Fact]
    public void RegistrarDiagnostico_AposRejeicao_SubstituiItensAntigos_ValorTotalCorreto()
    {
        var os = CriarOsAposRejeicao();
        var novoServicoId = Guid.NewGuid();
        var servicos = new[] { new ItemServicoInput(novoServicoId, 2, 150m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 3, 40m) };

        os.RegistrarDiagnostico("Novo diagnÃ³stico", servicos, pecas);

        Assert.Single(os.ItensServico);
        Assert.Single(os.ItensPeca);
        Assert.Equal(novoServicoId, os.ItensServico[0].ServicoId);
        Assert.Equal(420m, os.Orcamentos.Last().ValorTotal);
    }


    [Fact]
    public void EnviarOrcamento_AposRegeneracao_QuandoStatusJaEhAguardandoAprovacao_Sucede()
    {
        var os = CriarOsAposRejeicao();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };
        os.RegistrarDiagnostico("Novo diagnÃ³stico", servicos, pecas);

        var result = os.EnviarOrcamento(DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(StatusOrcamento.Enviado, os.Orcamentos.Last().Status);
    }


    [Fact]
    public void FluxoCompleto_RejeicaoSeguida_DeNovoOrcamentoAprovado_Sucede()
    {
        var os = CriarOsAposRejeicao();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };
        os.RegistrarDiagnostico("Segundo diagnÃ³stico", servicos, pecas);
        os.EnviarOrcamento(DateTime.UtcNow);

        var aprovacao = os.AprovarOrcamento();

        Assert.True(aprovacao.IsSuccess);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamentos.Last().Status);

        var execucao = os.Executar();
        Assert.True(execucao.IsSuccess);
        Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
    }


    [Fact]
    public void RegistrarDiagnostico_ComDadosValidos_AdicionaItens_CriaOrcamentoPendente_EmiteUmDiagnosticoConcluido()
    {
        var os = CriarOsEmDiagnostico();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 2, 50m) };

        var result = os.RegistrarDiagnostico("DiagnÃ³stico de teste", servicos, pecas);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
        Assert.Single(os.ItensServico);
        Assert.Single(os.ItensPeca);
        Assert.Single(os.Orcamentos);
        Assert.Equal(StatusOrcamento.Pendente, os.Orcamentos[0].Status);
        Assert.Single(os.DomainEvents);
        var evt = Assert.IsType<DiagnosticoConcluido>(os.DomainEvents.Single());
        Assert.Equal(os.Id, evt.OrdemServicoId);
        Assert.Equal("DiagnÃ³stico de teste", evt.DescricaoDiagnostico);
        Assert.Single(evt.Servicos);
        Assert.Single(evt.Pecas);
        Assert.Equal(200m, evt.ValorTotal);
    }

    [Fact]
    public void RegistrarDiagnostico_QuandoNaoEmDiagnostico_RetornaErroTransicaoInvalida()
    {
        var os = Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId).Value;
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };

        var result = os.RegistrarDiagnostico("desc", servicos, pecas);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.TransicaoInvalida", result.Error.Code);
    }

    [Fact]
    public void RegistrarDiagnostico_SemItensServico_RetornaErroOrcamentoSemServicos()
    {
        var os = CriarOsEmDiagnostico();
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };

        var result = os.RegistrarDiagnostico("desc", [], pecas);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.OrcamentoSemServicos", result.Error.Code);
    }

    [Fact]
    public void RegistrarDiagnostico_SemItensPeca_RetornaErroOrcamentoSemPecas()
    {
        var os = CriarOsEmDiagnostico();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };

        var result = os.RegistrarDiagnostico("desc", servicos, []);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.OrcamentoSemPecas", result.Error.Code);
    }

    [Fact]
    public void RejeitarOrcamento_ComOrcamentoEnviado_EmiteOrcamentoRejeitadoComPecasCorretas()
    {
        var os = CriarOsEmDiagnostico();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 2, 50m) };
        os.RegistrarDiagnostico("desc", servicos, pecas);
        os.EnviarOrcamento(DateTime.UtcNow);
        os.ClearDomainEvents();

        var result = os.RejeitarOrcamento();

        Assert.True(result.IsSuccess);
        Assert.Single(os.DomainEvents);
        var evt = Assert.IsType<OrcamentoRejeitado>(os.DomainEvents.Single());
        Assert.Equal(os.Id, evt.OrdemServicoId);
        Assert.Single(evt.Pecas);
        Assert.Equal(PecaId, evt.Pecas[0].PecaInsumoId);
        Assert.Equal(2, evt.Pecas[0].Quantidade);
        Assert.Equal(50m, evt.Pecas[0].PrecoUnitario);
    }

    [Fact]
    public void Finalizar_QuandoEmExecucao_EmiteOrdemServicoFinalizadaComClienteId()
    {
        var os = CriarOsEmDiagnostico();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };
        os.RegistrarDiagnostico("desc", servicos, pecas);
        os.EnviarOrcamento(DateTime.UtcNow);
        os.AprovarOrcamento();
        os.Executar();
        os.ClearDomainEvents();

        var result = os.Finalizar();

        Assert.True(result.IsSuccess);
        Assert.Single(os.DomainEvents);
        var evt = Assert.IsType<OrdemServicoFinalizada>(os.DomainEvents.Single());
        Assert.Equal(os.Id, evt.OrdemServicoId);
        Assert.Equal(ClienteId, evt.ClienteId);
    }
}
