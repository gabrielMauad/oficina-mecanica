using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.OrdemServico;

[Collection("Integration")]
public class AbrirOrdemServicoCompletaEndpointsTests
{
    private readonly OficinaMecanicaWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static HttpContent EmptyJsonContent() =>
        new StringContent("{}", Encoding.UTF8, "application/json");

    public AbrirOrdemServicoCompletaEndpointsTests(OficinaMecanicaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // =========================================================================
    // POST /api/v1/ordens-servico/completa
    // =========================================================================

    [Fact(DisplayName = "POST /completa: cria OS direto em AguardandoAprovacao, com orçamento Enviado e estoque reservado")]
    public async Task AbrirCompleta_ComDadosValidos_CriaOsAguardandoAprovacaoComOrcamentoEnviado()
    {
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        const int estoqueInicial = 10;
        const int quantidadeUsada = 3;

        var clienteId = await CriarClienteAsync(client,
            nome: "Fluxo Completo Cliente",
            documento: "98530209044",
            email: "fluxo.completo@integration.test",
            telefone: "31999990050");

        var veiculoId = await CriarVeiculoAsync(client,
            placa: "CMP1E23",
            modelo: "Civic",
            marca: "Honda",
            ano: 2022,
            clienteId: clienteId);

        var servicoId = await CriarServicoAsync(client,
            nome: "Alinhamento e Balanceamento OS Completa",
            preco: 120.00m);

        var pecaId = await CriarPecaAsync(client,
            nome: "Pastilha de Freio Completa",
            preco: 60.00m,
            estoque: estoqueInicial,
            unidade: "Par");

        // Act — abrir OS já com serviços e peças definidos
        var abrirResponse = await client.PostAsJsonAsync("/api/v1/ordens-servico/completa", new
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            Servicos = new[] { new { ServicoId = servicoId, Quantidade = 1 } },
            Pecas = new[] { new { PecaInsumoId = pecaId, Quantidade = quantidadeUsada } }
        });

        Assert.Equal(HttpStatusCode.Created, abrirResponse.StatusCode);
        Assert.NotNull(abrirResponse.Headers.Location);

        var os = await abrirResponse.Content.ReadFromJsonAsync<OrdemServicoDto>(JsonOptions);
        Assert.NotNull(os);
        Assert.Equal("AguardandoAprovacao", os.Status);
        Assert.Null(os.DescricaoDiagnostico);
        var osId = os.Id;

        // GET /{id}/status
        var statusResponse = await client.GetAsync($"/api/v1/ordens-servico/{osId}/status");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var status = await statusResponse.Content.ReadFromJsonAsync<StatusDto>(JsonOptions);
        Assert.Equal("AguardandoAprovacao", status!.Status);

        // Orçamento consta como Enviado (handler EnviarOrcamentoAoCliente reagiu ao evento)
        var osCompleta = await ObterOsAsync(client, osId);
        Assert.NotEmpty(osCompleta.Orcamentos);
        Assert.Equal("Enviado", osCompleta.Orcamentos[0].Status);

        // Peças reservadas via integration event (estoque decrementado)
        var peca = await ObterPecaAsync(client, pecaId);
        Assert.Equal(estoqueInicial - quantidadeUsada, peca.QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "POST /completa: OS aberta pelo fluxo novo reaproveita aprovar → executar → finalizar → concluir sem alterações")]
    public async Task AbrirCompleta_SeguidoDoCicloDeVidaCompleto_FuncionaSemAlteracoes()
    {
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var clienteId = await CriarClienteAsync(client,
            nome: "Ciclo Completo Cliente",
            documento: "16899269023",
            email: "ciclo.completo@integration.test",
            telefone: "31999990060");

        var veiculoId = await CriarVeiculoAsync(client,
            placa: "CIC2F34",
            modelo: "HB20",
            marca: "Hyundai",
            ano: 2020,
            clienteId: clienteId);

        var servicoId = await CriarServicoAsync(client,
            nome: "Revisão de Freios Ciclo Completo OS",
            preco: 90.00m);

        var pecaId = await CriarPecaAsync(client,
            nome: "Disco de Freio Dianteiro",
            preco: 45.00m,
            estoque: 5,
            unidade: "Unidade");

        var abrirResponse = await client.PostAsJsonAsync("/api/v1/ordens-servico/completa", new
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            Servicos = new[] { new { ServicoId = servicoId, Quantidade = 1 } },
            Pecas = new[] { new { PecaInsumoId = pecaId, Quantidade = 1 } }
        });
        abrirResponse.EnsureSuccessStatusCode();
        var osId = (await abrirResponse.Content.ReadFromJsonAsync<OrdemServicoDto>(JsonOptions))!.Id;

        var aprovarResponse = await client.PatchAsync(
            $"/api/v1/ordens-servico/{osId}/aprovar-orcamento", EmptyJsonContent());
        Assert.Equal(HttpStatusCode.OK, aprovarResponse.StatusCode);

        var executarResponse = await client.PatchAsync(
            $"/api/v1/ordens-servico/{osId}/executar", EmptyJsonContent());
        Assert.Equal(HttpStatusCode.OK, executarResponse.StatusCode);

        var finalizarResponse = await client.PatchAsync(
            $"/api/v1/ordens-servico/{osId}/finalizar", EmptyJsonContent());
        Assert.Equal(HttpStatusCode.OK, finalizarResponse.StatusCode);

        var concluirResponse = await client.PatchAsync(
            $"/api/v1/ordens-servico/{osId}/concluir", EmptyJsonContent());
        Assert.Equal(HttpStatusCode.OK, concluirResponse.StatusCode);

        var osFinal = await ObterOsAsync(client, osId);
        Assert.Equal("Entregue", osFinal.Status);
        Assert.NotNull(osFinal.EntregueEm);
    }

    // =========================================================================
    // GET /api/v1/ordens-servico/acompanhamento
    // =========================================================================

    [Fact(DisplayName = "GET /acompanhamento: retorna OS ativas ordenadas por prioridade, mais antigas primeiro, sem Finalizada/Entregue")]
    public async Task ListarParaAcompanhamento_RetornaApenasOsAtivasOrdenadasPorPrioridade()
    {
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var clienteId = await CriarClienteAsync(client,
            nome: "Acompanhamento Cliente",
            documento: "72809768080",
            email: "acompanhamento@integration.test",
            telefone: "31999990070");

        var veiculoId = await CriarVeiculoAsync(client,
            placa: "ACO3G45",
            modelo: "Onix",
            marca: "Chevrolet",
            ano: 2021,
            clienteId: clienteId);

        var servicoId = await CriarServicoAsync(client, nome: "Revisão Acompanhamento", preco: 70.00m);
        var pecaId = await CriarPecaAsync(client, nome: "Peça Acompanhamento", preco: 30.00m, estoque: 20, unidade: "Unidade");

        // OS 1: fica em Recebida
        var os1Id = await AbrirOsVaziaAsync(client, clienteId, veiculoId);

        // OS 2: avança até EmDiagnostico
        var os2Id = await AbrirOsVaziaAsync(client, clienteId, veiculoId);
        (await client.PatchAsync($"/api/v1/ordens-servico/{os2Id}/iniciar-diagnostico", EmptyJsonContent()))
            .EnsureSuccessStatusCode();

        // OS 3: aberta pelo fluxo novo → nasce direto em AguardandoAprovacao
        var os3Response = await client.PostAsJsonAsync("/api/v1/ordens-servico/completa", new
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            Servicos = new[] { new { ServicoId = servicoId, Quantidade = 1 } },
            Pecas = new[] { new { PecaInsumoId = pecaId, Quantidade = 1 } }
        });
        os3Response.EnsureSuccessStatusCode();
        var os3Id = (await os3Response.Content.ReadFromJsonAsync<OrdemServicoDto>(JsonOptions))!.Id;

        // OS 4: avança até Entregue → deve ficar de fora da listagem
        var os4Response = await client.PostAsJsonAsync("/api/v1/ordens-servico/completa", new
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            Servicos = new[] { new { ServicoId = servicoId, Quantidade = 1 } },
            Pecas = new[] { new { PecaInsumoId = pecaId, Quantidade = 1 } }
        });
        os4Response.EnsureSuccessStatusCode();
        var os4Id = (await os4Response.Content.ReadFromJsonAsync<OrdemServicoDto>(JsonOptions))!.Id;
        (await client.PatchAsync($"/api/v1/ordens-servico/{os4Id}/aprovar-orcamento", EmptyJsonContent())).EnsureSuccessStatusCode();
        (await client.PatchAsync($"/api/v1/ordens-servico/{os4Id}/executar", EmptyJsonContent())).EnsureSuccessStatusCode();
        (await client.PatchAsync($"/api/v1/ordens-servico/{os4Id}/finalizar", EmptyJsonContent())).EnsureSuccessStatusCode();
        (await client.PatchAsync($"/api/v1/ordens-servico/{os4Id}/concluir", EmptyJsonContent())).EnsureSuccessStatusCode();

        // Act
        var acompanhamentoResponse = await client.GetAsync("/api/v1/ordens-servico/acompanhamento");
        Assert.Equal(HttpStatusCode.OK, acompanhamentoResponse.StatusCode);

        var lista = await acompanhamentoResponse.Content.ReadFromJsonAsync<List<OrdemServicoDto>>(JsonOptions);
        Assert.NotNull(lista);

        // Entregue não aparece
        Assert.DoesNotContain(lista, x => x.Id == os4Id);

        var ids = lista.Select(x => x.Id).ToList();
        Assert.Contains(os1Id, ids);
        Assert.Contains(os2Id, ids);
        Assert.Contains(os3Id, ids);

        // Prioridade: AguardandoAprovacao (os3) antes de EmDiagnostico (os2) antes de Recebida (os1)
        var idx1 = ids.IndexOf(os1Id);
        var idx2 = ids.IndexOf(os2Id);
        var idx3 = ids.IndexOf(os3Id);
        Assert.True(idx3 < idx2, "AguardandoAprovacao deveria vir antes de EmDiagnostico");
        Assert.True(idx2 < idx1, "EmDiagnostico deveria vir antes de Recebida");
    }

    // ── Helpers privados ──

    private async Task<Guid> AbrirOsVaziaAsync(HttpClient client, Guid clienteId, Guid veiculoId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", new
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        });
        response.EnsureSuccessStatusCode();
        var os = await response.Content.ReadFromJsonAsync<OrdemServicoDto>(JsonOptions);
        return os!.Id;
    }

    private async Task<OrdemServicoDto> ObterOsAsync(HttpClient client, Guid osId)
    {
        var response = await client.GetAsync($"/api/v1/ordens-servico/{osId}");
        response.EnsureSuccessStatusCode();
        var os = await response.Content.ReadFromJsonAsync<OrdemServicoDto>(JsonOptions);
        return os!;
    }

    private async Task<PecaDto> ObterPecaAsync(HttpClient client, Guid pecaId)
    {
        var response = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PecaDto>(JsonOptions))!;
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

    // ── DTOs locais para desserialização ──

    private sealed record ClienteIdDto(Guid ClienteId);
    private sealed record VeiculoIdDto(Guid VeiculoId);
    private sealed record ServicoIdDto(Guid ServicoId);
    private sealed record PecaIdDto(Guid PecaInsumoId);

    private sealed record StatusDto(Guid Id, string Status);

    private sealed record PecaDto(
        Guid Id,
        string Nome,
        decimal PrecoUnitario,
        int QuantidadeEmEstoque,
        string UnidadeDeMedida,
        bool Ativo);

    private sealed record OrdemServicoDto(
        Guid Id,
        Guid ClienteId,
        Guid VeiculoId,
        string Status,
        string? DescricaoDiagnostico,
        DateTime? NotificadoEm,
        DateTime? EntregueEm,
        DateTime CriadoEm,
        DateTime AtualizadoEm,
        ItemServicoDto[] ItensServico,
        ItemPecaDto[] ItensPeca,
        OrcamentoDto[] Orcamentos);

    private sealed record ItemServicoDto(
        Guid ServicoId,
        int Quantidade,
        decimal PrecoUnitarioSnapshot);

    private sealed record ItemPecaDto(
        Guid PecaInsumoId,
        int Quantidade,
        decimal PrecoUnitarioSnapshot);

    private sealed record OrcamentoDto(
        decimal ValorTotal,
        string Status,
        DateTime DataGeracao,
        DateTime? DataEnvio,
        DateTime? DataAprovacao);
}
