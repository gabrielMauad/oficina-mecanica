# Plano de Tarefas — Oficina Mecânica MVP

> Ordem baseada na arquitetura definida em `docs/arquitetura/estrutura-do-projeto.md`.
> Os 25 projetos já estão scaffoldados — todas as tarefas são de implementação.
> Complete e valide cada tarefa antes de avançar para a próxima.

---

## Fase 1 — Fundação

### T01 — SharedKernel.Domain

Implementar os tipos-base que todos os módulos vão herdar e usar: `Entity<TId>`, `AggregateRoot<TId>` (com suporte a domain events), `ValueObject`, `IDomainEvent`, `IIntegrationEvent`, `Result<T>` e `Error`.

**Validação:** Projeto compila sem erros. Nenhuma referência a EF Core ou qualquer pacote de infraestrutura.

---

### T02 — SharedKernel.Application

Implementar as abstrações transversais: `IIntegrationEventBus`, `IIntegrationEventHandler<T>`, `InMemoryIntegrationEventBus` (via `IServiceProvider`), e os pipeline behaviors do MediatR: `ValidationBehavior`, `LoggingBehavior` e `TransactionBehavior`.

**Validação:** Projeto compila. `InMemoryIntegrationEventBus` consegue publicar para múltiplos handlers registrados.

---

### T03 — Bootstrap/Api — Esqueleto

Configurar o `Program.cs` mínimo: Swagger (sem auth ainda), health check em `/health`, exception middleware global retornando Problem Details (RFC 7807). Deixar os registros de módulos como placeholders comentados.

**Validação:** `dotnet run` sobe sem erro. `GET /healthz` retorna 200. Scalar acessível em `/scalar`.

---

### T04 — Dockerfile + docker-compose

Criar o `Dockerfile` multi-stage em `src/Bootstrap/Api/` (build no `sdk`, runtime no `aspnet`). Criar o `docker-compose.yml` na raiz com os serviços `postgres` (postgres:16, volume persistente) e `api` (build do Dockerfile, depende do postgres), com as variáveis de ambiente necessárias para a connection string.

**Validação:** `docker compose up --build` sobe os dois containers sem erro. `GET /healthz` retorna 200 através do container da api.

---

## Fase 2 — Módulo Cadastro

> Primeiro módulo fim-a-fim. Objetivo: validar que toda a pilha funciona antes de replicar para os demais módulos.

### T05 — Cadastro.Domain

Implementar os três agregados do bounded context:

- `Cliente` — nome, documento (CPF ou CNPJ), email, telefone, ativo. Emite `ClienteCadastrado`.
- `Veiculo` — placa (Mercosul `ABC1D23` ou padrão antigo `ABC-1234`), modelo, marca, ano, clienteId. Emite `VeiculoCadastrado`.
- `Servico` — nome, descrição, preço base, ativo. Emite `ServicoCadastrado`.

Value Objects com validação: `Cpf`, `Cnpj`, `Placa`, `Dinheiro`.

Interfaces de repositório: `IClienteRepository`, `IVeiculoRepository`, `IServicoRepository`.

**Validação:** Projeto compila sem referência a EF Core. CPF inválido, CNPJ inválido e placa fora dos dois formatos lançam erro ao tentar criar o value object.

---

### T06 — Cadastro.Contracts

Definir a interface pública do módulo (o que outros módulos podem consumir):

- `ICadastroClienteQuery` — `ObterPorId(Guid)` retornando `ClienteResumoDto`
- `ICadastroVeiculoQuery` — `ObterPorId(Guid)` retornando `VeiculoResumoDto`
- `ICadastroServicoQuery` — `ObterPorId(Guid)` retornando `ServicoResumoDto`
- DTOs correspondentes com os campos necessários para os outros módulos
- Integration events: `ClienteCadastradoIntegrationEvent`, `VeiculoCadastradoIntegrationEvent`

**Validação:** Projeto compila. Única referência de projeto permitida é `SharedKernel.Domain`.

---

### T07 — Cadastro.Application

Implementar os use cases com CQRS (MediatR v12), um por pasta (Command + Handler + Validator + Response):

- `CadastrarCliente`, `CadastrarVeiculo`, `AdicionarServico`
- `AtualizarCliente`, `AtualizarVeiculo`, `AtualizarServico` (edição dos dados básicos)
- `DesativarCliente`, `DesativarServico` (soft delete via flag `ativo`)
- Queries: `ObterClientePorId`, `ObterVeiculoPorId`, `ObterServicoPorId`, `ListarClientes`, `ListarVeiculos`, `ListarServicos`, `ListarVeiculosPorCliente`

Handlers de comando publicam o integration event correspondente via `IIntegrationEventBus` após persistir.

**Validação:** Projeto compila. Referências externas limitadas a `Cadastro.Domain`, `SharedKernel.*`.

---

### T08 — Cadastro.Infrastructure

- `CadastroDbContext` com `HasDefaultSchema("cadastro")` e mapeamentos das 3 entidades conforme `docs/arquitetura/database-schema.md`
- Migration EF Core criando as tabelas `cliente`, `veiculo` (com FK para `cliente`) e `servico`
- Implementações dos repositórios: `ClienteRepository`, `VeiculoRepository`, `ServicoRepository`
- Implementações das queries públicas: `CadastroClienteQuery`, `CadastroVeiculoQuery`, `CadastroServicoQuery`
- `CadastroModule.cs` com o método `AddCadastroModule(IServiceCollection, IConfiguration)` registrando tudo

**Validação:** `dotnet ef migrations add InitialCreate` gera o migration corretamente. Com Postgres rodando, `dotnet ef database update` cria o schema `cadastro` com as três tabelas.

---

### T09 — Cadastro.Presentation

Controllers REST com os endpoints de cada agregado:

- `ClientesController` — `POST /clientes`, `GET /clientes`, `GET /clientes/{id}`, `PUT /clientes/{id}`, `DELETE /clientes/{id}` (desativa)
- `VeiculosController` — `POST /veiculos`, `GET /veiculos`, `GET /veiculos/{id}`, `GET /clientes/{id}/veiculos`, `PUT /veiculos/{id}`
- `ServicosController` — `POST /servicos`, `GET /servicos`, `GET /servicos/{id}`, `PUT /servicos/{id}`, `DELETE /servicos/{id}` (desativa)

**Validação:** Todos os endpoints aparecem no Swagger. CRUD completo de cliente funciona via Swagger UI com Postgres rodando.

---

### T10 — Testes — Cadastro.Domain.Tests

Testes unitários sem IO e sem mocks:

- CPF com dígitos verificadores válidos e inválidos
- CNPJ com dígitos verificadores válidos e inválidos
- Placa no formato Mercosul e no formato antigo (válidos e inválidos)
- Criação de `Cliente` com dados válidos → domain event `ClienteCadastrado` gerado
- Criação de `Veiculo` com dados válidos → domain event `VeiculoCadastrado` gerado
- Criação de `Servico` → domain event `ServicoCadastrado` gerado

**Validação:** `dotnet test` passa 100%. Cobertura de `Cadastro.Domain` >= 80%.

---

### T11 — Testes — Cadastro.Application.Tests

Testes dos handlers com mocks dos repositórios (Moq):

- `CadastrarClienteHandler` persiste e publica integration event
- `CadastrarClienteHandler` retorna erro quando documento já existe
- `CadastrarVeiculoHandler` persiste e publica integration event
- `CadastrarVeiculoHandler` retorna erro quando placa já existe
- Handlers de query retornam `null` (ou Result de erro) quando entidade não existe

**Validação:** `dotnet test` passa 100%.

---

### T12 — Integrar Cadastro no Bootstrap

Chamar `AddCadastroModule(configuration)` no `Program.cs` e registrar os controllers de `Cadastro.Presentation` via `AddApplicationPart`. Documentar o comando de migration manual no README (ou configurar auto-migration no startup).

**Validação:** `docker compose up`. `POST /clientes` cria um cliente. `GET /clientes/{id}` retorna o cliente criado.

---

## Fase 3 — Módulo PecasInsumos

> Sem dependências de outros BCs — implementar antes de OrdemServico para já ter os contratos disponíveis.

### T13 — PecasInsumos.Domain

Implementar o agregado `PecaInsumo` (nome, descrição, preço unitário, quantidade em estoque, unidade de medida, ativo). Domain events: `PecaInsumoAdicionada`, `EstoqueAtualizado`, `EstoqueEsgotado`. Interface `IPecaInsumoRepository`.

Regra de negócio crítica: estoque não pode ficar negativo — o método de decremento deve validar disponibilidade antes de alterar.

**Validação:** Projeto compila sem referências de infra. Tentativa de decrementar estoque abaixo de zero lança erro de domínio.

---

### T14 — PecasInsumos.Contracts

- `IPecasInsumosDisponibilidadeQuery` — `VerificarDisponibilidade(Guid pecaId, int quantidade)` retornando `DisponibilidadeDto`
- `IPecaInsumoQuery` — `ObterPorId(Guid)` retornando `PecaInsumoResumoDto`
- DTOs correspondentes
- `EstoqueDecrementadoIntegrationEvent`

**Validação:** Projeto compila. Única referência de projeto é `SharedKernel.Domain`.

---

### T15 — PecasInsumos.Application

- `AdicionarPecaInsumo` (Command + Handler + Validator + Response)
- `AtualizarPecaInsumo` (dados básicos)
- `IncrementarEstoque`, `DecrementarEstoque` (Commands separados)
- `DesativarPecaInsumo`
- Queries: `ObterPecaInsumoPorId`, `ListarPecasInsumos`, `BuscarPecasPorNome`
- Handler `DecrementarEstoqueQuandoOrcamentoAprovado` (reage ao `OrcamentoAprovadoIntegrationEvent` do módulo OrdemServico) — implementar já, será conectado na Fase 5

> **Nota sobre o fluxo do event storming:** o event storming mostra o estoque sendo decrementado no momento em que as peças são vinculadas à OS (durante o diagnóstico). Para o MVP, simplificamos: o estoque é verificado em `RegistrarDiagnostico` via ACL port (sem decremento), e decrementado apenas quando o orçamento é aprovado (T27). Isso reduz a complexidade sem comprometer os requisitos do Tech Challenge.

**Validação:** Projeto compila. Handler de decremento por integration event existe e implementado, mesmo que não conectado ainda.

---

### T16 — PecasInsumos.Infrastructure

- `PecasInsumosDbContext` com `HasDefaultSchema("pecas_insumos")`
- Migration para a tabela `peca_insumo` conforme database-schema.md
- `PecaInsumoRepository`
- Implementações das queries públicas: `PecasInsumosDisponibilidadeQuery`, `PecaInsumoQuery`
- `PecasInsumosModule.cs` com `AddPecasInsumosModule(IServiceCollection, IConfiguration)`

**Validação:** Migration cria a tabela `pecas_insumos.peca_insumo` corretamente.

---

### T17 — PecasInsumos.Presentation

`PecasInsumosController`:
- `POST /pecas-insumos` (adicionar)
- `GET /pecas-insumos`, `GET /pecas-insumos/{id}` (listar e buscar)
- `PUT /pecas-insumos/{id}` (atualizar dados básicos)
- `PATCH /pecas-insumos/{id}/estoque` (incrementar ou decrementar)
- `DELETE /pecas-insumos/{id}` (desativar)

**Validação:** Endpoints aparecem no Swagger. CRUD básico funciona com Postgres rodando.

---

### T18 — Testes — PecasInsumos

- `PecasInsumos.Domain.Tests`: decremento válido, decremento que esgota o estoque, tentativa de decremento abaixo de zero, criação com dados válidos gera domain event correto
- `PecasInsumos.Application.Tests`: handlers com mocks do repositório

**Validação:** `dotnet test` passa 100%. Cobertura de `PecasInsumos.Domain` >= 80%.

---

### T19 — Integrar PecasInsumos no Bootstrap

`AddPecasInsumosModule` + `AddApplicationPart` no `Program.cs`.

**Validação:** `docker compose up`. `POST /pecas-insumos` cria uma peça. `GET /pecas-insumos` lista.

---

## Fase 4 — Módulo OrdemServico

> Módulo mais complexo. Depende de Cadastro e PecasInsumos via ACL.

### T20 — OrdemServico.Domain

Agregado `OrdemServico` com o ciclo de vida completo mapeado no event storming:

**Estados:** `Recebida → EmDiagnostico → OrcamentoPendente → OrcamentoEnviado → OrcamentoAprovado → EmExecucao → Finalizada → Entregue`

Entidades filhas: `ItemServico` (snapshot de preço), `ItemPeca` (snapshot de preço). Agregado `Orcamento` (valor total, status: Pendente/Enviado/Aprovado/Rejeitado).

Domain events para cada transição: `OrdemServicoGerada`, `DiagnosticoIniciado`, `DiagnosticoRegistrado`, `OrcamentoGerado`, `OrcamentoEnviado`, `OrcamentoAprovado`, `OrdemServicoEmExecucao`, `OrdemServicoFinalizada`, `ClienteNotificado`, `OrdemServicoConcluida`.

Ports de ACL (interfaces no vocabulário deste módulo): `IClienteInfoPort`, `IVeiculoInfoPort`, `IServicoInfoPort`, `IPecaDisponibilidadePort`.

Interfaces: `IOrdemServicoRepository`.

**Validação:** Projeto compila. Transições inválidas (ex: tentar finalizar uma OS `Recebida`) lançam erro de domínio. Referências de projeto limitadas a `SharedKernel.Domain`.

---

### T21 — OrdemServico.Contracts

- `IOrdemServicoResumoQuery` — `ObterPorId(Guid)` retornando `OrdemServicoResumoDto`
- `IListarOrdensPorClienteQuery` — `Listar(Guid clienteId)` retornando `IReadOnlyList<OrdemServicoResumoDto>`
- DTOs: `OrdemServicoResumoDto`, `OrcamentoDto`, `ItemServicoDto`, `ItemPecaDto`
- Integration events: `OrcamentoAprovadoIntegrationEvent` (com lista de itens e quantidades), `OrdemServicoFinalizadaIntegrationEvent`

**Validação:** Projeto compila. Única referência de projeto é `SharedKernel.Domain`.

---

### T22 — OrdemServico.Application

Implementar todos os commands do event storming, um por pasta:

| Command | O que faz |
|---|---|
| `GerarOrdemServico` | Valida que cliente e veículo existem via ACL; cria a OS com status `Recebida` |
| `ReceberOrdemServico` | Transição de status (policy automática) |
| `IniciarDiagnostico` | Muda status para `EmDiagnostico` |
| `RegistrarDiagnostico` | Registra serviços e peças com snapshot de preço; verifica disponibilidade das peças via ACL |
| `GerarOrcamento` | Calcula valor total a partir dos itens; cria o `Orcamento` com status `Pendente` |
| `EnviarOrcamento` | Muda status do orçamento para `Enviado`; preenche `data_envio` |
| `AprovarOrcamento` | Muda status para `Aprovado`; publica `OrcamentoAprovadoIntegrationEvent` |
| `RejeitarOrcamento` | Muda status do orçamento para `Rejeitado`; OS volta para `OrcamentoPendente` para permitir reenvio |
| `ExecutarOrdemServico` | Muda status da OS para `EmExecucao` |
| `FinalizarOrdemServico` | Muda status para `Finalizada`; publica `OrdemServicoFinalizadaIntegrationEvent` |
| `NotificarCliente` | Preenche `notificado_em` |
| `ConcluirOrdemServico` | Muda status para `Entregue`; preenche `entregue_em` |

Queries: `ObterOrdemServicoPorId`, `ListarOrdensPorCliente`.

**Validação:** Projeto compila. Handlers usam apenas as ports de ACL definidas no Domain — nenhuma referência direta a `Cadastro.*` ou `PecasInsumos.*`.

---

### T23 — OrdemServico.Infrastructure

- `OrdemServicoDbContext` com `HasDefaultSchema("ordem_servico")` e mapeamentos das 4 tabelas
- Migrations sem FKs cross-schema (apenas FKs entre `os_servico`, `os_peca`, `orcamento` → `ordem_servico`)
- `OrdemServicoRepository`
- ACL Adapters (implementam as ports do Domain consumindo os Contracts dos outros módulos):
  - `ClienteInfoAdapter` consome `ICadastroClienteQuery`
  - `VeiculoInfoAdapter` consome `ICadastroVeiculoQuery`
  - `ServicoInfoAdapter` consome `ICadastroServicoQuery`
  - `PecaDisponibilidadeAdapter` consome `IPecasInsumosDisponibilidadeQuery`
- Implementação de `IOrdemServicoResumoQuery` e `IListarOrdensPorClienteQuery`
- `OrdemServicoModule.cs` com `AddOrdemServicoModule(IServiceCollection, IConfiguration)`

**Validação:** Migration cria as 4 tabelas sem FKs cross-schema. Adapters traduzem os DTOs externos para os tipos internos do módulo.

---

### T24 — OrdemServico.Presentation

`OrdensServicoController` com todos os endpoints do ciclo de vida:

- `POST /ordens-servico` — criar OS
- `PATCH /ordens-servico/{id}/receber`
- `PATCH /ordens-servico/{id}/iniciar-diagnostico`
- `PATCH /ordens-servico/{id}/registrar-diagnostico` (body com serviços e peças)
- `PATCH /ordens-servico/{id}/gerar-orcamento`
- `PATCH /ordens-servico/{id}/enviar-orcamento`
- `PATCH /ordens-servico/{id}/aprovar-orcamento`
- `PATCH /ordens-servico/{id}/executar`
- `PATCH /ordens-servico/{id}/finalizar`
- `PATCH /ordens-servico/{id}/notificar-cliente`
- `PATCH /ordens-servico/{id}/concluir`
- `GET /ordens-servico/{id}`
- `GET /ordens-servico?clienteId={id}`

**Validação:** Todos os endpoints aparecem no Swagger. Fluxo completo de uma OS (criar → concluir) funciona via chamadas sequenciais no Swagger UI.

---

### T25 — Testes — OrdemServico

- `OrdemServico.Domain.Tests`: cada transição de estado válida e inválida, cálculo de valor total do orçamento, geração dos domain events em cada transição, regra de snapshot de preço nos itens
- `OrdemServico.Application.Tests`: handlers com mocks das ports ACL e repositórios, verificar que `OrcamentoAprovadoIntegrationEvent` é publicado no handler correto

**Validação:** `dotnet test` passa 100%. Cobertura de `OrdemServico.Domain` >= 80%.

---

### T26 — Integrar OrdemServico no Bootstrap

`AddOrdemServicoModule` + `AddApplicationPart` no `Program.cs`.

**Validação:** `docker compose up`. Fluxo completo de uma OS funciona do início (`POST /ordens-servico`) até o fim (`PATCH /concluir`).

---

## Fase 5 — Integration Events

### T27 — OrcamentoAprovado → DecrementarEstoque

Garantir que o handler `DecrementarEstoqueQuandoOrcamentoAprovado` (já implementado em T15) está registrado como `IIntegrationEventHandler<OrcamentoAprovadoIntegrationEvent>` no `AddPecasInsumosModule`. Verificar que o handler de `AprovarOrcamento` em OrdemServico está publicando o evento corretamente via `IIntegrationEventBus`.

**Validação:** Aprovar um orçamento que contém peças decrementa o `quantidade_estoque` correspondente em `pecas_insumos.peca_insumo`. Verificar diretamente no banco após a aprovação.

---

### T28 — OSFinalizada → Notificação (stub)

Garantir que o handler de `FinalizarOrdemServico` publica `OrdemServicoFinalizadaIntegrationEvent`. Criar um handler stub que apenas loga a mensagem "OS {id} finalizada — cliente deve ser notificado" e registrá-lo no módulo.

**Validação:** Finalizar uma OS gera a mensagem de log. Nenhum erro lançado.

---

## Fase 6 — Segurança

### T29 — JWT nos endpoints administrativos

Configurar `AddAuthentication().AddJwtBearer(...)` no Bootstrap com o segredo via variável de ambiente. Anotar com `[Authorize]` todos os controllers. Adicionar o esquema Bearer ao Swagger (botão "Authorize" na UI). Incluir o segredo JWT no `docker-compose.yml`.

Endpoints públicos (sem `[Authorize]`): `GET /ordens-servico/{id}` (acompanhamento pelo cliente), `GET /health`.

**Validação:** Chamada sem token retorna 401. Chamada com token válido retorna 200. Swagger mostra cadeado nos endpoints protegidos e permite inserir o token pelo botão Authorize.

---

## Fase 7 — Qualidade e Entrega

### T30 — Testes de Integração

Implementar no projeto `tests/IntegrationTests`:

- `WebApplicationFactoryFixture` com Testcontainers.PostgreSql (sobe Postgres real por sessão)
- Testes ponta-a-ponta em `Modules/Cadastro/`: criar cliente, buscar cliente, criar veículo
- Testes ponta-a-ponta em `Modules/PecasInsumos/`: adicionar peça, verificar estoque
- Testes ponta-a-ponta em `Modules/OrdemServico/`: fluxo completo de uma OS
- Teste cross-module em `EventBus/`: aprovar orçamento → verificar que o estoque da peça foi decrementado

**Validação:** `dotnet test tests/IntegrationTests` passa com Postgres real. Sem uso de InMemory provider ou mocks de banco.

---

### T31 — Verificar e Atingir Cobertura >= 80%

Rodar Coverlet nos três projetos Domain. Gerar relatório HTML com ReportGenerator. Identificar e preencher as lacunas de cobertura com testes unitários adicionais até atingir 80% em cada Domain.

**Validação:** Relatório mostra >= 80% de cobertura de linha em `Cadastro.Domain`, `OrdemServico.Domain` e `PecasInsumos.Domain`.

---

### T32 — README

Escrever o `README.md` na raiz com:

- Visão geral do sistema e da arquitetura (Modular Monolith, 3 BCs)
- `docker compose up --build` para subir tudo
- Comando para rodar as migrations de cada módulo
- Comando para rodar os testes unitários e de integração
- Como acessar o Swagger
- Como gerar um token JWT para testar os endpoints protegidos (usuário e segredo de teste)

**Validação:** Alguém sem contexto prévio consegue rodar o projeto e testar os endpoints seguindo apenas o README.

---

## Checklist — Requisitos do Tech Challenge

| Requisito do PDF | Tarefa(s) |
|---|---|
| Back-end com DDD aplicado | T05, T13, T20 |
| APIs RESTful | T09, T17, T24 |
| Documentação via Swagger | T03, T29 |
| Autenticação JWT nas APIs administrativas | T29 |
| Validação de CPF/CNPJ | T05, T10 |
| Validação de placa | T05, T10 |
| Testes unitários | T10, T11, T18, T25 |
| Testes de integração | T30 |
| Cobertura >= 80% nos domínios críticos | T31 |
| Dockerfile | T04 |
| docker-compose | T04 |
| README para execução local | T32 |
