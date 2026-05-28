using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.EventBus;

[Collection("Integration")]
public class EstoqueIntegrationEventTests
{
    private readonly OficinaMecanicaWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Retorna um novo HttpContent com body vazio para PATCHes sem payload.
    /// HttpContent é descartável — não reutilizar a mesma instância entre requests.
    /// </summary>
    private static HttpContent EmptyJsonContent() =>
        new StringContent("{}", Encoding.UTF8, "application/json");

    public EstoqueIntegrationEventTests(OficinaMecanicaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "OrcamentoGerado → estoque é decrementado automaticamente via integration event")]
    public async Task RegistrarDiagnostico_DeveDecrementarEstoque_ViaIntegrationEvent()
    {
        // ──────────────────────────────────────────────────────────────
        // ARRANGE — preparar todos os pré-requisitos
        // ──────────────────────────────────────────────────────────────

        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        const int estoqueInicial = 10;
        const int quantidadeUsada = 3;
        const int estoqueEsperadoApos = estoqueInicial - quantidadeUsada; // 7

        // 1. Criar peça com estoque inicial conhecido
        var pecaId = await CriarPecaAsync(client,
            nome: "Vela de Ignição NGK EventBus",
            preco: 12.50m,
            estoque: estoqueInicial,
            unidade: "Unidade");

        // 2. Criar cliente
        var clienteId = await CriarClienteAsync(client,
            nome: "Ana EventBus",
            documento: "23157499050",
            email: "ana.eventbus@integration.test",
            telefone: "31999990020");

        // 3. Criar veículo
        var veiculoId = await CriarVeiculoAsync(client,
            placa: "DEF3G45",
            modelo: "Gol",
            marca: "Volkswagen",
            ano: 2019,
            clienteId: clienteId);

        // 4. Criar serviço no catálogo (obrigatório para RegistrarDiagnostico)
        var servicoId = await CriarServicoAsync(client,
            nome: "Troca de Velas",
            preco: 80.00m);

        // ──────────────────────────────────────────────────────────────
        // ACT — executar o fluxo até RegistrarDiagnostico
        // ──────────────────────────────────────────────────────────────

        // 5. Criar OS
        var osResponse = await client.PostAsJsonAsync("/api/v1/ordens-servico", new
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        });
        osResponse.EnsureSuccessStatusCode();
        var os = await osResponse.Content.ReadFromJsonAsync<IdDto>(JsonOptions);
        var osId = os!.Id;

        // 6. Iniciar Diagnóstico
        var iniciarResponse = await client.PatchAsync(
            $"/api/v1/ordens-servico/{osId}/iniciar-diagnostico", EmptyJsonContent());
        iniciarResponse.EnsureSuccessStatusCode();

        // 7. Registrar Diagnóstico — este é o passo que dispara o integration event
        //    O handler DecrementarEstoqueQuandoOrcamentoGerado vai decrementar o estoque
        var registrarBody = new
        {
            DescricaoDiagnostico = "Velas com desgaste excessivo. Substituir todas.",
            Servicos = new[] { new { ServicoId = servicoId, Quantidade = 1 } },
            Pecas = new[] { new { PecaInsumoId = pecaId, Quantidade = quantidadeUsada } }
        };

        var registrarResponse = await client.PatchAsJsonAsync(
            $"/api/v1/ordens-servico/{osId}/registrar-diagnostico", registrarBody);
        Assert.Equal(HttpStatusCode.OK, registrarResponse.StatusCode);

        // ──────────────────────────────────────────────────────────────
        // ASSERT — verificar que o estoque foi decrementado no banco
        // ──────────────────────────────────────────────────────────────

        // O integration event é processado de forma síncrona (InMemoryIntegrationEventBus)
        // durante o mesmo request — não é necessário aguardar
        var pecaResponse = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}");
        Assert.Equal(HttpStatusCode.OK, pecaResponse.StatusCode);

        var peca = await pecaResponse.Content.ReadFromJsonAsync<PecaDto>(JsonOptions);
        Assert.NotNull(peca);
        Assert.Equal(estoqueEsperadoApos, peca.QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "OrcamentoRejeitado → estoque é estornado via integration event")]
    public async Task RejeitarOrcamento_DeveEstornarEstoque_ViaIntegrationEvent()
    {
        // ──────────────────────────────────────────────────────────────
        // ARRANGE
        // ──────────────────────────────────────────────────────────────

        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        const int estoqueInicial = 20;
        const int quantidadeUsada = 5;

        // 1. Criar peça com estoque conhecido
        var pecaId = await CriarPecaAsync(client,
            nome: "Correia Dentada Gates EventBus",
            preco: 95.00m,
            estoque: estoqueInicial,
            unidade: "Unidade");

        // 2. Pré-requisitos — CPF diferente para evitar conflito de unicidade no banco compartilhado
        var clienteId = await CriarClienteAsync(client,
            nome: "Bruno Rejeicao",
            documento: "52998224059",
            email: "bruno.rejeicao@integration.test",
            telefone: "31999990030");

        var veiculoId = await CriarVeiculoAsync(client,
            placa: "REJ1A12",
            modelo: "Sandero",
            marca: "Renault",
            ano: 2018,
            clienteId: clienteId);

        var servicoId = await CriarServicoAsync(client,
            nome: "Troca de Correia Dentada",
            preco: 200.00m);

        // ──────────────────────────────────────────────────────────────
        // ACT — fluxo até registrar + rejeitar
        // ──────────────────────────────────────────────────────────────

        // Criar OS
        var osResponse = await client.PostAsJsonAsync("/api/v1/ordens-servico", new
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        });
        osResponse.EnsureSuccessStatusCode();
        var osId = (await osResponse.Content.ReadFromJsonAsync<IdDto>(JsonOptions))!.Id;

        // Iniciar Diagnóstico
        (await client.PatchAsync($"/api/v1/ordens-servico/{osId}/iniciar-diagnostico", EmptyJsonContent()))
            .EnsureSuccessStatusCode();

        // Registrar Diagnóstico → decrementa estoque (20 - 5 = 15)
        var registrarBody = new
        {
            DescricaoDiagnostico = "Correia dentada com risco de ruptura.",
            Servicos = new[] { new { ServicoId = servicoId, Quantidade = 1 } },
            Pecas = new[] { new { PecaInsumoId = pecaId, Quantidade = quantidadeUsada } }
        };

        (await client.PatchAsJsonAsync(
            $"/api/v1/ordens-servico/{osId}/registrar-diagnostico", registrarBody))
            .EnsureSuccessStatusCode();

        // Verificar que o estoque foi decrementado para 15
        var pecaAposDecremento = await ObterPecaAsync(client, pecaId);
        Assert.Equal(estoqueInicial - quantidadeUsada, pecaAposDecremento.QuantidadeEmEstoque); // 15

        // Rejeitar Orçamento → deve estornar o estoque (15 + 5 = 20)
        (await client.PatchAsync(
            $"/api/v1/ordens-servico/{osId}/rejeitar-orcamento", EmptyJsonContent()))
            .EnsureSuccessStatusCode();

        // ──────────────────────────────────────────────────────────────
        // ASSERT — estoque deve ter voltado ao valor inicial
        // ──────────────────────────────────────────────────────────────

        var pecaAposEstorno = await ObterPecaAsync(client, pecaId);
        Assert.Equal(estoqueInicial, pecaAposEstorno.QuantidadeEmEstoque); // 20 novamente
    }

    // ── Helpers privados ──

    private async Task<PecaDto> ObterPecaAsync(HttpClient client, Guid pecaId)
    {
        var response = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PecaDto>(JsonOptions))!;
    }

    private async Task<Guid> CriarPecaAsync(HttpClient client,
        string nome, decimal preco, int estoque, string unidade)
    {
        var response = await client.PostAsJsonAsync("/api/v1/pecas-insumos", new
        {
            Nome = nome,
            Descricao = (string?)null,
            Preco = preco,
            QuantidadeEmEstoque = estoque,
            UnidadeDeMedida = unidade
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PecaIdDto>(JsonOptions);
        return body!.PecaInsumoId;
    }

    private async Task<Guid> CriarClienteAsync(HttpClient client,
        string nome, string documento, string email, string telefone)
    {
        var response = await client.PostAsJsonAsync("/api/v1/clientes", new
        {
            Nome = nome,
            Documento = documento,
            Email = email,
            Telefone = telefone,
            PessoaFisica = true
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ClienteIdDto>(JsonOptions);
        return body!.ClienteId;
    }

    private async Task<Guid> CriarVeiculoAsync(HttpClient client,
        string placa, string modelo, string marca, int ano, Guid clienteId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/veiculos", new
        {
            Placa = placa,
            Modelo = modelo,
            Marca = marca,
            Ano = ano,
            ClienteId = clienteId
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<VeiculoIdDto>(JsonOptions);
        return body!.VeiculoId;
    }

    private async Task<Guid> CriarServicoAsync(HttpClient client, string nome, decimal preco)
    {
        var response = await client.PostAsJsonAsync("/api/v1/servicos", new
        {
            Nome = nome,
            Descricao = (string?)null,
            Preco = preco
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ServicoIdDto>(JsonOptions);
        return body!.ServicoId;
    }

    // DTOs locais
    private sealed record IdDto(Guid Id);
    private sealed record PecaIdDto(Guid PecaInsumoId);
    private sealed record ClienteIdDto(Guid ClienteId);
    private sealed record VeiculoIdDto(Guid VeiculoId);
    private sealed record ServicoIdDto(Guid ServicoId);

    private sealed record PecaDto(
        Guid Id,
        string Nome,
        decimal PrecoUnitario,
        int QuantidadeEmEstoque,
        string UnidadeDeMedida,
        bool Ativo);
}
