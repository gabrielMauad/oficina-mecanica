using OrdensServico.Domain.OrdemServico;
using OrdensServico.Domain.OrdemServico.Events;

namespace OrdensServico.Domain.Tests.OrdemServico;

public class OrdemServicoAbrirComServicosTests
{
    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ServicoId = Guid.NewGuid();
    private static readonly Guid PecaId = Guid.NewGuid();

    [Fact(DisplayName = "AbrirComServicos: dados válidos → OS AguardandoAprovacao, orçamento Pendente, sem descrição, evento OrcamentoGerado")]
    public void AbrirComServicos_ComDadosValidos_CriaOsAguardandoAprovacaoComOrcamentoPendente()
    {
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 2, 50m) };

        var result = Domain.OrdemServico.OrdemServico.AbrirComServicos(ClienteId, VeiculoId, servicos, pecas);

        Assert.True(result.IsSuccess);
        var os = result.Value;
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(ClienteId, os.ClienteId);
        Assert.Equal(VeiculoId, os.VeiculoId);
        Assert.Null(os.DescricaoDiagnostico);
        Assert.Single(os.ItensServico);
        Assert.Single(os.ItensPeca);
        Assert.Single(os.Orcamentos);
        Assert.Equal(StatusOrcamento.Pendente, os.Orcamentos[0].Status);
        Assert.Equal(200m, os.Orcamentos[0].ValorTotal);

        Assert.Single(os.DomainEvents);
        var evt = Assert.IsType<OrcamentoGerado>(os.DomainEvents.Single());
        Assert.Equal(os.Id, evt.OrdemServicoId);
        Assert.Null(evt.DescricaoDiagnostico);
    }

    [Fact(DisplayName = "AbrirComServicos: sem serviços → erro OrcamentoSemServicos")]
    public void AbrirComServicos_SemServicos_RetornaErroOrcamentoSemServicos()
    {
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };

        var result = Domain.OrdemServico.OrdemServico.AbrirComServicos(ClienteId, VeiculoId, [], pecas);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.OrcamentoSemServicos", result.Error.Code);
    }

    [Fact(DisplayName = "AbrirComServicos: sem peças → erro OrcamentoSemPecas")]
    public void AbrirComServicos_SemPecas_RetornaErroOrcamentoSemPecas()
    {
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };

        var result = Domain.OrdemServico.OrdemServico.AbrirComServicos(ClienteId, VeiculoId, servicos, []);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.OrcamentoSemPecas", result.Error.Code);
    }

    [Fact(DisplayName = "AbrirComServicos: ClienteId vazio → erro ClienteIdVazio")]
    public void AbrirComServicos_ComClienteIdVazio_RetornaErroClienteIdVazio()
    {
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };

        var result = Domain.OrdemServico.OrdemServico.AbrirComServicos(Guid.Empty, VeiculoId, servicos, pecas);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.ClienteIdVazio", result.Error.Code);
    }

    [Fact(DisplayName = "AbrirComServicos: VeiculoId vazio → erro VeiculoIdVazio")]
    public void AbrirComServicos_ComVeiculoIdVazio_RetornaErroVeiculoIdVazio()
    {
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 1, 50m) };

        var result = Domain.OrdemServico.OrdemServico.AbrirComServicos(ClienteId, Guid.Empty, servicos, pecas);

        Assert.True(result.IsFailure);
        Assert.Equal("OrdemServico.VeiculoIdVazio", result.Error.Code);
    }

    [Fact(DisplayName = "Regressão v1: RegistrarDiagnostico continua com comportamento idêntico após a extração do helper")]
    public void RegistrarDiagnostico_AposExtracaoDoHelper_MantemComportamentoOriginal()
    {
        var os = Domain.OrdemServico.OrdemServico.Criar(ClienteId, VeiculoId).Value;
        os.IniciarDiagnostico();
        var servicos = new[] { new ItemServicoInput(ServicoId, 1, 100m) };
        var pecas = new[] { new ItemPecaInput(PecaId, 2, 50m) };

        var result = os.RegistrarDiagnostico("Descrição do diagnóstico", servicos, pecas);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
        Assert.Equal("Descrição do diagnóstico", os.DescricaoDiagnostico);
        Assert.Single(os.Orcamentos);
        Assert.Equal(StatusOrcamento.Pendente, os.Orcamentos[0].Status);
        Assert.Equal(200m, os.Orcamentos[0].ValorTotal);

        var evt = Assert.IsType<OrcamentoGerado>(os.DomainEvents.Single());
        Assert.Equal("Descrição do diagnóstico", evt.DescricaoDiagnostico);
    }
}
