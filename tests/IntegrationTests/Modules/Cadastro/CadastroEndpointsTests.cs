using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.Cadastro;

[Collection("Integration")]
public class CadastroEndpointsTests
{
    private readonly OficinaMecanicaWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CadastroEndpointsTests(OficinaMecanicaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // =========================================================================
    // POST /api/v1/clientes
    // =========================================================================

    [Fact(DisplayName = "POST /clientes — retorna 201 com dados do cliente criado")]
    public async Task CriarCliente_DeveRetornar201_QuandoDadosValidos()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "João Silva",
            Documento = "01404238000",
            Email = "joao.silva@integration.test",
            Telefone = "31999990001",
            PessoaFisica = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/clientes", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.ClienteId);
        Assert.Equal("João Silva", body.Nome);
        Assert.Equal("01404238000", body.Documento);
        Assert.Equal("joao.silva@integration.test", body.Email);
        Assert.True(body.Ativo);
    }

    [Fact(DisplayName = "POST /clientes sem token — retorna 401")]
    public async Task CriarCliente_SemAutenticacao_DeveRetornar401()
    {
        // Arrange — client sem token
        using var client = _factory.CreateClient();

        var command = new
        {
            Nome = "Anônimo",
            Documento = "12345678909",
            Email = "anonimo@test.com",
            Telefone = "31999990000",
            PessoaFisica = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/clientes", command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =========================================================================
    // GET /api/v1/clientes
    // =========================================================================

    [Fact(DisplayName = "GET /clientes — retorna 200 com lista contendo o cliente criado")]
    public async Task ListarClientes_DeveRetornar200_ComListaDeItens()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Maria Santos",
            Documento = "52998224725",
            Email = "maria.santos@integration.test",
            Telefone = "31999990002",
            PessoaFisica = true
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/clientes", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);

        // Act
        var response = await client.GetAsync("/api/v1/clientes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<ClienteListItemResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body);

        var cliente = body.FirstOrDefault(c => c.Id == created!.ClienteId);
        Assert.NotNull(cliente);
        Assert.Equal("Maria Santos", cliente.Nome);
        Assert.Equal("52998224725", cliente.Documento);
    }

    // =========================================================================
    // GET /api/v1/clientes/{id}
    // =========================================================================

    [Fact(DisplayName = "GET /clientes/{id} — retorna 200 com dados do cliente")]
    public async Task ObterCliente_DeveRetornar200_QuandoClienteExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Pedro Costa",
            Documento = "87748248800",
            Email = "pedro.costa@integration.test",
            Telefone = "31999990003",
            PessoaFisica = true
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/clientes", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);
        var clienteId = created!.ClienteId;

        // Act
        var response = await client.GetAsync($"/api/v1/clientes/{clienteId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ClienteGetResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(clienteId, body.Id);
        Assert.Equal("Pedro Costa", body.Nome);
        Assert.Equal("87748248800", body.Documento);
        Assert.Equal("pedro.costa@integration.test", body.Email);
        Assert.True(body.Ativo);
    }

    [Fact(DisplayName = "GET /clientes/{id} inexistente — retorna 404")]
    public async Task ObterCliente_DeveRetornar404_QuandoNaoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/clientes/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // PATCH /api/v1/clientes/{id}/nome
    // =========================================================================

    [Fact(DisplayName = "PATCH /clientes/{id}/nome — atualiza nome e retorna 200")]
    public async Task AtualizarNomeCliente_DeveRetornar200_ComNovoNome()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Carlos Antigo",
            Documento = "26475244406",
            Email = "carlos@integration.test",
            Telefone = "31999990004",
            PessoaFisica = true
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/clientes", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);
        var clienteId = created!.ClienteId;

        // Act
        var patchContent = JsonContent.Create(new { Nome = "Carlos Novo" });
        var response = await client.PatchAsync($"/api/v1/clientes/{clienteId}/nome", patchContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ClienteAtualizadoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(clienteId, body.ClienteId);
        Assert.Equal("Carlos Novo", body.Nome);
    }

    // =========================================================================
    // PATCH /api/v1/clientes/{id}/telefone
    // =========================================================================

    [Fact(DisplayName = "PATCH /clientes/{id}/telefone — atualiza telefone e retorna 200")]
    public async Task AtualizarTelefoneCliente_DeveRetornar200_ComNovoTelefone()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Ana Lima",
            Documento = "71428793860",
            Email = "ana.lima@integration.test",
            Telefone = "31999990005",
            PessoaFisica = true
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/clientes", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);
        var clienteId = created!.ClienteId;

        // Act
        var patchContent = JsonContent.Create(new { Telefone = "31988880005" });
        var response = await client.PatchAsync($"/api/v1/clientes/{clienteId}/telefone", patchContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ClienteAtualizadoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(clienteId, body.ClienteId);
        Assert.Equal("31988880005", body.Telefone);
    }

    // =========================================================================
    // DELETE /api/v1/clientes/{id}
    // =========================================================================

    [Fact(DisplayName = "DELETE /clientes/{id} — desativa o cliente e retorna 204")]
    public async Task DesativarCliente_DeveRetornar204_EClienteFinicaInativo()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Lucas Temporário",
            Documento = "98765432100",
            Email = "lucas@integration.test",
            Telefone = "31999990006",
            PessoaFisica = true
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/clientes", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);
        var clienteId = created!.ClienteId;

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/v1/clientes/{clienteId}");

        // Assert — 204 No Content
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Soft delete: cliente ainda existe mas ativo = false
        var getResponse = await client.GetAsync($"/api/v1/clientes/{clienteId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content.ReadFromJsonAsync<ClienteGetResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Ativo);
    }

    [Fact(DisplayName = "DELETE /clientes/{id} inexistente — retorna 404")]
    public async Task DesativarCliente_DeveRetornar404_QuandoNaoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.DeleteAsync($"/api/v1/clientes/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // POST /api/v1/veiculos
    // =========================================================================

    [Fact(DisplayName = "POST /veiculos — retorna 201 com dados do veículo criado")]
    public async Task CriarVeiculo_DeveRetornar201_QuandoDadosValidos()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var clienteCommand = new
        {
            Nome = "Bruno Motorista",
            Documento = "11144477735",
            Email = "bruno@integration.test",
            Telefone = "31999990007",
            PessoaFisica = true
        };

        // Tenta criar o cliente; se CPF já existe (de outro teste), busca pelo GET da lista
        var clienteResponse = await client.PostAsJsonAsync("/api/v1/clientes", clienteCommand);
        Guid clienteId;

        if (clienteResponse.StatusCode == HttpStatusCode.Created)
        {
            var clienteCriado = await clienteResponse.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);
            clienteId = clienteCriado!.ClienteId;
        }
        else
        {
            // CPF já cadastrado — busca na lista
            var listResponse = await client.GetAsync("/api/v1/clientes");
            listResponse.EnsureSuccessStatusCode();
            var lista = await listResponse.Content.ReadFromJsonAsync<List<ClienteListItemResponse>>(JsonOptions);
            clienteId = lista!.First(c => c.Documento == "11144477735").Id;
        }

        var veiculoCommand = new
        {
            Placa = "ABC1D23",
            Modelo = "Civic",
            Marca = "Honda",
            Ano = 2022,
            ClienteId = clienteId
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/veiculos", veiculoCommand);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<VeiculoCriadoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.VeiculoId);
        Assert.Equal("ABC1D23", body.Placa);
        Assert.Equal("Civic", body.Modelo);
        Assert.Equal("Honda", body.Marca);
        Assert.Equal(2022, body.Ano);
        Assert.Equal(clienteId, body.ClienteId);
    }

    // =========================================================================
    // GET /api/v1/veiculos
    // =========================================================================

    [Fact(DisplayName = "GET /veiculos — retorna 200 com lista contendo o veículo criado")]
    public async Task ListarVeiculos_DeveRetornar200_ComListaDeItens()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Criar cliente dono do veículo
        var clienteCommand = new
        {
            Nome = "Diego Viajante",
            Documento = "23456789173",
            Email = "diego@integration.test",
            Telefone = "31999990008",
            PessoaFisica = true
        };

        var clienteResponse = await client.PostAsJsonAsync("/api/v1/clientes", clienteCommand);
        clienteResponse.EnsureSuccessStatusCode();
        var clienteCriado = await clienteResponse.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);

        var veiculoCommand = new
        {
            Placa = "GHI1J23",
            Modelo = "Corolla",
            Marca = "Toyota",
            Ano = 2021,
            ClienteId = clienteCriado!.ClienteId
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/veiculos", veiculoCommand);
        createResponse.EnsureSuccessStatusCode();
        var veiculoCriado = await createResponse.Content.ReadFromJsonAsync<VeiculoCriadoResponse>(JsonOptions);

        // Act
        var response = await client.GetAsync("/api/v1/veiculos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<VeiculoListItemResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body);

        var veiculo = body.FirstOrDefault(v => v.Id == veiculoCriado!.VeiculoId);
        Assert.NotNull(veiculo);
        Assert.Equal("GHI1J23", veiculo.Placa);
        Assert.Equal("Corolla", veiculo.Modelo);
    }

    // =========================================================================
    // GET /api/v1/veiculos/{id}
    // =========================================================================

    [Fact(DisplayName = "GET /veiculos/{id} — retorna 200 com dados do veículo")]
    public async Task ObterVeiculo_DeveRetornar200_QuandoVeiculoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var clienteCommand = new
        {
            Nome = "Fernanda Condutora",
            Documento = "34567891228",
            Email = "fernanda@integration.test",
            Telefone = "31999990009",
            PessoaFisica = true
        };

        var clienteResponse = await client.PostAsJsonAsync("/api/v1/clientes", clienteCommand);
        clienteResponse.EnsureSuccessStatusCode();
        var clienteCriado = await clienteResponse.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);

        var veiculoCommand = new
        {
            Placa = "LMN2O34",
            Modelo = "Fit",
            Marca = "Honda",
            Ano = 2020,
            ClienteId = clienteCriado!.ClienteId
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/veiculos", veiculoCommand);
        createResponse.EnsureSuccessStatusCode();
        var veiculoCriado = await createResponse.Content.ReadFromJsonAsync<VeiculoCriadoResponse>(JsonOptions);
        var veiculoId = veiculoCriado!.VeiculoId;

        // Act
        var response = await client.GetAsync($"/api/v1/veiculos/{veiculoId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<VeiculoGetResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(veiculoId, body.Id);
        Assert.Equal("LMN2O34", body.Placa);
        Assert.Equal("Fit", body.Modelo);
        Assert.Equal("Honda", body.Marca);
        Assert.Equal(2020, body.Ano);
        Assert.Equal(clienteCriado.ClienteId, body.ClienteId);
    }

    [Fact(DisplayName = "GET /veiculos/{id} inexistente — retorna 404")]
    public async Task ObterVeiculo_DeveRetornar404_QuandoNaoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/veiculos/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // GET /api/v1/clientes/{id}/veiculos
    // =========================================================================

    [Fact(DisplayName = "GET /clientes/{id}/veiculos — retorna 200 com veículos do cliente")]
    public async Task ListarVeiculosPorCliente_DeveRetornar200_ComVeiculosDoCliente()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var clienteCommand = new
        {
            Nome = "Gabriel Frota",
            Documento = "45678912364",
            Email = "gabriel@integration.test",
            Telefone = "31999990010",
            PessoaFisica = true
        };

        var clienteResponse = await client.PostAsJsonAsync("/api/v1/clientes", clienteCommand);
        clienteResponse.EnsureSuccessStatusCode();
        var clienteCriado = await clienteResponse.Content.ReadFromJsonAsync<ClienteCriadoResponse>(JsonOptions);
        var clienteId = clienteCriado!.ClienteId;

        // Criar dois veículos para este cliente
        foreach (var placa in new[] { "PQR3S45", "TUV4W56" })
        {
            var veiculoCommand = new
            {
                Placa = placa,
                Modelo = "Onix",
                Marca = "Chevrolet",
                Ano = 2023,
                ClienteId = clienteId
            };
            var v = await client.PostAsJsonAsync("/api/v1/veiculos", veiculoCommand);
            v.EnsureSuccessStatusCode();
        }

        // Act
        var response = await client.GetAsync($"/api/v1/clientes/{clienteId}/veiculos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<VeiculosPorClienteResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(clienteId, body.ClienteId);
        Assert.Equal("Gabriel Frota", body.NomeCliente);
        Assert.Equal(2, body.Veiculos.Count);
        Assert.Contains(body.Veiculos, v => v.Placa == "PQR3S45");
        Assert.Contains(body.Veiculos, v => v.Placa == "TUV4W56");
    }

    [Fact(DisplayName = "GET /clientes/{id}/veiculos — retorna 404 quando cliente não existe")]
    public async Task ListarVeiculosPorCliente_DeveRetornar404_QuandoClienteNaoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/clientes/{Guid.NewGuid()}/veiculos");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // POST /api/v1/servicos
    // =========================================================================

    [Fact(DisplayName = "POST /servicos — retorna 201 com dados do serviço criado")]
    public async Task CriarServico_DeveRetornar201_QuandoDadosValidos()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Troca de Óleo",
            Descricao = "Troca completa com filtro",
            Preco = 150.00m
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/servicos", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ServicoCriadoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.ServicoId);
        Assert.Equal("Troca de Óleo", body.Nome);
        Assert.Equal("Troca completa com filtro", body.Descricao);
        Assert.Equal(150.00m, body.PrecoBase);
        Assert.True(body.Ativo);
    }

    // =========================================================================
    // GET /api/v1/servicos
    // =========================================================================

    [Fact(DisplayName = "GET /servicos — retorna 200 com lista contendo o serviço criado")]
    public async Task ListarServicos_DeveRetornar200_ComListaDeItens()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Alinhamento e Balanceamento",
            Descricao = (string?)null,
            Preco = 120.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/servicos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ServicoCriadoResponse>(JsonOptions);

        // Act
        var response = await client.GetAsync("/api/v1/servicos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<ServicoListItemResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body);

        var servico = body.FirstOrDefault(s => s.Id == created!.ServicoId);
        Assert.NotNull(servico);
        Assert.Equal("Alinhamento e Balanceamento", servico.Nome);
        Assert.Equal(120.00m, servico.Preco);
    }

    // =========================================================================
    // GET /api/v1/servicos/{id}
    // =========================================================================

    [Fact(DisplayName = "GET /servicos/{id} — retorna 200 com dados do serviço")]
    public async Task ObterServico_DeveRetornar200_QuandoServicoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Revisão de Freios",
            Descricao = "Pastilhas e discos dianteiros",
            Preco = 350.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/servicos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ServicoCriadoResponse>(JsonOptions);
        var servicoId = created!.ServicoId;

        // Act
        var response = await client.GetAsync($"/api/v1/servicos/{servicoId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ServicoGetResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(servicoId, body.Id);
        Assert.Equal("Revisão de Freios", body.Nome);
        Assert.Equal("Pastilhas e discos dianteiros", body.Descricao);
        Assert.Equal(350.00m, body.Preco);
        Assert.True(body.Ativo);
    }

    [Fact(DisplayName = "GET /servicos/{id} inexistente — retorna 404")]
    public async Task ObterServico_DeveRetornar404_QuandoNaoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/servicos/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // PATCH /api/v1/servicos/{id}/descricao
    // =========================================================================

    [Fact(DisplayName = "PATCH /servicos/{id}/descricao — atualiza descrição e retorna 200")]
    public async Task AtualizarDescricaoServico_DeveRetornar200_ComNovaDescricao()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Diagnóstico Eletrônico",
            Descricao = "Descrição original",
            Preco = 200.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/servicos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ServicoCriadoResponse>(JsonOptions);
        var servicoId = created!.ServicoId;

        // Act
        var patchContent = JsonContent.Create(new { Descricao = "Leitura de falhas com scanner" });
        var response = await client.PatchAsync($"/api/v1/servicos/{servicoId}/descricao", patchContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ServicoAtualizadoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(servicoId, body.ServicoId);
        Assert.Equal("Leitura de falhas com scanner", body.Descricao);
    }

    // =========================================================================
    // PATCH /api/v1/servicos/{id}/preco
    // =========================================================================

    [Fact(DisplayName = "PATCH /servicos/{id}/preco — atualiza preço e retorna 200")]
    public async Task AtualizarPrecoServico_DeveRetornar200_ComNovoPreco()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Lavagem Completa",
            Descricao = (string?)null,
            Preco = 80.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/servicos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ServicoCriadoResponse>(JsonOptions);
        var servicoId = created!.ServicoId;

        // Act
        var patchContent = JsonContent.Create(new { Preco = 95.00m });
        var response = await client.PatchAsync($"/api/v1/servicos/{servicoId}/preco", patchContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ServicoAtualizadoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(servicoId, body.ServicoId);
        Assert.Equal(95.00m, body.Preco);
    }

    // =========================================================================
    // DELETE /api/v1/servicos/{id}
    // =========================================================================

    [Fact(DisplayName = "DELETE /servicos/{id} — desativa o serviço e retorna 204")]
    public async Task DesativarServico_DeveRetornar204_EServicoFicaInativo()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = "Polimento Temporário",
            Descricao = (string?)null,
            Preco = 300.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/servicos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ServicoCriadoResponse>(JsonOptions);
        var servicoId = created!.ServicoId;

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/v1/servicos/{servicoId}");

        // Assert — 204 No Content
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Soft delete: serviço ainda existe mas ativo = false
        var getResponse = await client.GetAsync($"/api/v1/servicos/{servicoId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content.ReadFromJsonAsync<ServicoGetResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Ativo);
    }

    [Fact(DisplayName = "DELETE /servicos/{id} inexistente — retorna 404")]
    public async Task DesativarServico_DeveRetornar404_QuandoNaoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.DeleteAsync($"/api/v1/servicos/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // DTOs locais para desserialização
    // Padrão do projeto: POSTs retornam *Id, GETs retornam Id
    // =========================================================================

    // --- Clientes ---
    private sealed record ClienteCriadoResponse(
        Guid ClienteId,
        string Nome,
        string Documento,
        string Email,
        string Telefone,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    private sealed record ClienteGetResponse(
        Guid Id,
        string Nome,
        string Documento,
        string Email,
        string Telefone,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    private sealed record ClienteListItemResponse(
        Guid Id,
        string Nome,
        string Documento,
        string Email,
        string Telefone,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    // PATCH /nome e /telefone → "clienteId"
    private sealed record ClienteAtualizadoResponse(
        Guid ClienteId,
        string Nome,
        string Telefone,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    // --- Veículos ---
    private sealed record VeiculoCriadoResponse(
        Guid VeiculoId,
        string Placa,
        string Modelo,
        string Marca,
        int Ano,
        Guid ClienteId,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    private sealed record VeiculoGetResponse(
        Guid Id,
        string Placa,
        string Modelo,
        string Marca,
        int Ano,
        Guid ClienteId,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    private sealed record VeiculoListItemResponse(
        Guid Id,
        string Placa,
        string Modelo,
        string Marca,
        int Ano,
        Guid ClienteId,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    private sealed record VeiculosPorClienteResponse(
        Guid ClienteId,
        string NomeCliente,
        IReadOnlyList<VeiculoDoClienteResponse> Veiculos);

    private sealed record VeiculoDoClienteResponse(
        Guid VeiculoId,
        string Placa,
        string Modelo,
        string Marca,
        int Ano,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    // --- Serviços ---
    private sealed record ServicoCriadoResponse(
        Guid ServicoId,
        string Nome,
        string? Descricao,
        decimal PrecoBase,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    private sealed record ServicoGetResponse(
        Guid Id,
        string Nome,
        string? Descricao,
        decimal Preco,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    private sealed record ServicoListItemResponse(
        Guid Id,
        string Nome,
        string? Descricao,
        decimal Preco,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    // PATCH /descricao e /preco → "servicoId"
    private sealed record ServicoAtualizadoResponse(
        Guid ServicoId,
        string? Descricao,
        decimal Preco,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);
}
