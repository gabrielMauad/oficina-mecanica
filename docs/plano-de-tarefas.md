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

> **Decisão de design — fluxo pós-commit em duas etapas:**
>
> O `TransactionBehavior` orquestra a seguinte sequência após o handler retornar com sucesso:
> 1. Coleta os domain events acumulados nos agregados via `IUnitOfWork.CollectDomainEvents()`.
> 2. Persiste com `SaveChangesAsync()` e limpa os domain events do agregado.
> 3. Despacha cada domain event via `IPublisher` (MediatR) — os handlers de domain events são chamados aqui.
> 4. Domain event handlers podem enfileirar integration events em `IPendingIntegrationEvents`.
> 5. Itera `IPendingIntegrationEvents` e publica cada evento via `IIntegrationEventBus`.
>
> A separação entre etapas 3 e 5 garante: (a) nenhum evento sai antes do commit; (b) a lógica de publicação de integration events fica nos handlers de domain events, não nos command handlers.
>
> `IUnitOfWork` expõe `CollectDomainEvents()` e `ClearDomainEvents()` além do `SaveChangesAsync`. `IHasDomainEvents` é a interface que os agregados implementam (via `AggregateRoot`). `IDomainEvent` implementa `INotification` do MediatR para ser despachado via `IPublisher`.

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

- `Cliente` — nome, documento (CPF ou CNPJ), email, telefone, ativo.
- `Veiculo` — placa (Mercosul `ABC1D23` ou padrão antigo `ABC-1234`), modelo, marca, ano, clienteId.
- `Servico` — nome, descrição, preço base, ativo.

Value Objects com validação: `Cpf`, `Cnpj`, `Placa`, `Dinheiro`.

Interfaces de repositório: `IClienteRepository`, `IVeiculoRepository`, `IServicoRepository`.

> **Decisão de design — sem domain events em cadastro:** os agregados de Cadastro não emitem domain events. Nenhum consumidor existe (e nunca existirá dentro do monolito) para `ClienteCadastrado`, `VeiculoCadastrado` ou `ServicoCadastrado`. Evento sem consumidor é dead code: aumenta complexidade sem benefício. DDD real não exige events em todo agregado — apenas quando há reação real a modelar.

**Validação:** Projeto compila sem referência a EF Core. CPF inválido, CNPJ inválido e placa fora dos dois formatos lançam erro ao tentar criar o value object.

---

### T06 — Cadastro.Contracts

Definir a interface pública do módulo (o que outros módulos podem consumir):

- `ICadastroClienteQuery` — `ObterPorId(Guid)` retornando `ClienteResumoDto`
- `ICadastroVeiculoQuery` — `ObterPorId(Guid)` retornando `VeiculoResumoDto`
- `ICadastroServicoQuery` — `ObterPorId(Guid)` retornando `ServicoResumoDto`
- DTOs correspondentes com os campos necessários para os outros módulos
**Validação:** Projeto compila. Única referência de projeto permitida é `SharedKernel.Domain`.

---

### T07 — Cadastro.Application

Implementar os use cases com CQRS (MediatR v12), um por pasta (Command + Handler + Validator + Response):

- `CadastrarCliente`, `CadastrarVeiculo`, `AdicionarServico`
- `AtualizarCliente` (PATCH parcial — Nome e Telefone independentes), `AtualizarServico` (PATCH parcial — Descricao e Preco independentes)
- `DesativarCliente`, `DesativarServico` (soft delete via flag `ativo`)
- Queries: `ObterClientePorId`, `ObterVeiculoPorId`, `ObterServicoPorId`, `ListarClientes`, `ListarVeiculos`, `ListarServicos`, `ListarVeiculosPorCliente`

> **Decisão de design — Veiculo é imutável:** o agregado `Veiculo` não possui métodos de mutação. Placa, modelo, marca e ano são definidos na criação e não podem ser alterados. O use case `AtualizarVeiculo` não existe — esta é uma decisão intencional de domínio, não um item pendente.

> **Decisão de design — AtualizarCliente usa PATCH parcial:** Como Documento e Email são imutáveis após o cadastro, um `PUT /clientes/{id}` nunca substituiria o recurso inteiro — portanto a semântica PUT não se aplica. A atualização é exposta como dois endpoints `PATCH` independentes (`PATCH /clientes/{id}/nome` e `PATCH /clientes/{id}/telefone`), cada um com seu próprio command. O command `AtualizarClienteCommand` declara `Nome` e `Telefone` como `string?`; campos `null` são ignorados pelo handler (nenhuma alteração aplicada). O validator usa `.When(x => x.Campo is not null)` para só validar formato quando o campo é fornecido.

> **Decisão de design — AtualizarServico usa PATCH parcial:** O `Nome` é imutável após o cadastro (identifica o serviço no catálogo). Como o recurso nunca pode ser substituído por inteiro, `PUT /servicos/{id}` não se aplica. A atualização é exposta como dois endpoints `PATCH` independentes: `PATCH /servicos/{id}/descricao` e `PATCH /servicos/{id}/preco`. O command `AtualizarServicoCommand` declara `Descricao` e `Preco` como tipos anuláveis; campos `null` são ignorados pelo handler. O validator usa `.When(x => x.Campo is not null)` para só validar formato quando o campo é fornecido.

> **Decisão de design — response DTOs:** DTOs de Contracts (ex.: `ClienteDto`) podem ser reutilizados como tipo de retorno de queries quando o shape bate exatamente. Quando o caso de uso precisa de mais campos, define-se um tipo próprio na Application (ex.: `ObterClientePorIdResponse`). Tipos de retorno de commands (ex.: `CadastrarClienteResponse`) são sempre definidos na Application.

> **Decisão de design — queries de listagem:** os métodos de listagem (`ListarClientes`, `ListarVeiculos`, `ListarServicos`, `ListarVeiculosPorCliente`) NÃO devem ser adicionados às interfaces de repositório de domínio (`IClienteRepository`, `IVeiculoRepository`, `IServicoRepository`). Repositórios de domínio existem para servir invariantes de agregado, não projeções de leitura. Em vez disso, definir interfaces de leitura dentro da própria camada Application (ex: em cada pasta de query ou em um subdiretório `ReadModel`). Infrastructure implementa essas interfaces com projeção direta via `DbContext` para DTOs planos.

**Validação:** Projeto compila. Referências externas limitadas a `Cadastro.Domain`, `Cadastro.Contracts`, `SharedKernel.*`.

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

- `ClientesController` — `POST /clientes`, `GET /clientes`, `GET /clientes/{id}`, `PATCH /clientes/{id}/nome`, `PATCH /clientes/{id}/telefone`, `DELETE /clientes/{id}` (desativa)
- `VeiculosController` — `POST /veiculos`, `GET /veiculos`, `GET /veiculos/{id}`, `GET /clientes/{id}/veiculos` (sem `PUT` — Veiculo é imutável por decisão de domínio)
- `ServicosController` — `POST /servicos`, `GET /servicos`, `GET /servicos/{id}`, `PATCH /servicos/{id}/descricao`, `PATCH /servicos/{id}/preco`, `DELETE /servicos/{id}` (desativa)

**Validação:** Todos os endpoints aparecem no Swagger. CRUD completo de cliente funciona via Swagger UI com Postgres rodando.

---

### T10 — Testes — Cadastro.Domain.Tests

Testes unitários sem IO e sem mocks:

- CPF com dígitos verificadores válidos e inválidos
- CNPJ com dígitos verificadores válidos e inválidos
- Placa no formato Mercosul e no formato antigo (válidos e inválidos)
- Criação de `Cliente`, `Veiculo` e `Servico` com dados válidos e inválidos

**Validação:** `dotnet test` passa 100%. Cobertura de `Cadastro.Domain` >= 80%.

---

### T11 — Testes — Cadastro.Application.Tests

Testes dos handlers com mocks dos repositórios (Moq):

- `CadastrarClienteHandler` persiste com sucesso
- `CadastrarClienteHandler` retorna erro quando documento já existe
- `CadastrarVeiculoHandler` persiste com sucesso
- `CadastrarVeiculoHandler` retorna erro quando placa já existe
- Handlers de query retornam `null` (ou Result de erro) quando entidade não existe

**Validação:** `dotnet test` passa 100%.

---

### T12 — Integrar Cadastro no Bootstrap

Chamar `AddCadastroModule(configuration)` no `Program.cs` e registrar os controllers de `Cadastro.Presentation` via `AddApplicationPart`. Documentar o comando de migration manual no README (ou configurar auto-migration no startup).

Criar o método `AddSharedKernelServices(this IServiceCollection services)` em `SharedKernel.Application` registrando `IPendingIntegrationEvents → PendingIntegrationEvents` (Scoped) e `IIntegrationEventBus → InMemoryIntegrationEventBus` (Singleton). Chamar esse método no `Program.cs` e remover o registro de `IPendingIntegrationEvents` do `CadastroModule`.

**Validação:** `docker compose up`. `POST /clientes` cria um cliente. `GET /clientes/{id}` retorna o cliente criado.

---

## Fase 3 — Módulo PecasInsumos

> Sem dependências de outros BCs — implementar antes de OrdemServico para já ter os contratos disponíveis.

### T13 — PecasInsumos.Domain

Implementar o agregado `PecaInsumo` (nome, descrição, preço unitário, quantidade em estoque, unidade de medida, ativo). Interface `IPecaInsumoRepository`.

> **Decisão de design — sem domain events:** `PecaInsumo` não emite domain events. Os eventos de estoque (`EstoqueAtualizado`, `EstoqueEsgotado`) foram descartados por não terem consumidores dentro do monolito. O módulo PecasInsumos reage a integration events de outros módulos (ex.: `OrcamentoGeradoIntegrationEvent`) mas não gera events próprios.

Regra de negócio crítica: estoque não pode ficar negativo — o método de decremento deve validar disponibilidade antes de alterar.

**Validação:** Projeto compila sem referências de infra. Tentativa de decrementar estoque abaixo de zero lança erro de domínio.

---

### T14 — PecasInsumos.Contracts

- `IPecasInsumosDisponibilidadeQuery` — `VerificarDisponibilidade(Guid pecaId, int quantidade)` retornando `DisponibilidadeDto`
- `IPecaInsumoQuery` — `ObterPorId(Guid)` retornando `PecaInsumoResumoDto`
- DTOs correspondentes
**Validação:** Projeto compila. Única referência de projeto é `SharedKernel.Domain`.

---

### T15 — PecasInsumos.Application

- `AdicionarPecaInsumo` (Command + Handler + Validator + Response)
- `AtualizarPecaInsumo` (dados básicos)
- `IncrementarEstoque`, `DecrementarEstoque` (Commands separados)
- `DesativarPecaInsumo`
- Queries: `ObterPecaInsumoPorId`, `ListarPecasInsumos`
> **Nota sobre o fluxo de estoque:** o estoque é decrementado no momento em que o orçamento é gerado (ao concluir o diagnóstico). O domain event `DiagnosticoConcluido` dispara a publicação do `OrcamentoGeradoIntegrationEvent`, que o módulo PecasInsumos consome para decrementar o estoque. Se o orçamento for rejeitado, o `OrcamentoRejeitadoIntegrationEvent` aciona o estorno. A implementação do handler de decremento fica na T27.

> **Nota sobre disponibilidade de peças:** verificar disponibilidade via ACL port (`IPecaDisponibilidadePort`) no handler de `RegistrarDiagnostico` antes de criar os itens — falha rápida se alguma peça não tem estoque suficiente.

**Validação:** Projeto compila. Todos os commands e queries listados acima estão implementados.

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

- `PecasInsumos.Domain.Tests`: decremento válido, decremento que esgota o estoque, tentativa de decremento abaixo de zero, criação com dados válidos e inválidos
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

Agregado `OrdemServico` com o ciclo de vida completo mapeado no event storming.

**Estados:** `Recebida → EmDiagnostico → AguardandoAprovacao → EmExecucao → Finalizada → Entregue`

Entidades filhas: `ItemServico` (snapshot de preço), `ItemPeca` (snapshot de preço). Entidade `Orcamento` (valor total, status: Pendente/Enviado/Aprovado/Rejeitado).

**Métodos do agregado:**
- `Criar(clienteId, veiculoId)` — cria OS com status `Recebida`.
- `IniciarDiagnostico()` — transição para `EmDiagnostico`.
- `RegistrarDiagnostico(string desc, IEnumerable<ItemServicoInput> servicos, IEnumerable<ItemPecaInput> pecas)` — registra todos os itens, calcula total, cria `Orcamento` com `Status = Pendente`. OS permanece em `EmDiagnostico`. Emite **`DiagnosticoConcluido`** com payload rico (desc, snapshots de itens, valorTotal, orcamentoId).
- `EnviarOrcamento(dataEnvio)` — muda orçamento para `Enviado`, preenche `data_envio`, transita OS para `AguardandoAprovacao`. Sem evento. Chamado pelo handler `EnviarOrcamentoAoCliente` (T22).
- `AprovarOrcamento()` — aprova o orçamento (sem event — sem consumidor imediato).
- `RejeitarOrcamento()` — rejeita o orçamento. Emite **`OrcamentoRejeitado`** com lista de peças (para estorno de estoque).
- `Executar()` — transição para `EmExecucao`.
- `Finalizar()` — transição para `Finalizada`. Emite **`OrdemServicoFinalizada`** com `ClienteId` no payload.
- `NotificarCliente(dataNotificacao)` — registra `notificado_em`.
- `Concluir(dataEntrega)` — transição para `Entregue`, registra `entregue_em`.

**Input types:** `ItemServicoInput(ServicoId, Quantidade, PrecoUnitario)` e `ItemPecaInput(PecaInsumoId, Quantidade, PrecoUnitario)`.

**Domain events (apenas os que têm consumidores):**
- `DiagnosticoConcluido` — payload: `OrdemServicoId`, `OrcamentoId`, `DescricaoDiagnostico`, `IReadOnlyList<ItemServicoSnapshot>`, `IReadOnlyList<ItemPecaSnapshot>`, `ValorTotal`, `OcorridoEm`.
- `OrcamentoRejeitado` — payload: `OrdemServicoId`, `OrcamentoId`, `IReadOnlyList<ItemPecaSnapshot>`, `OcorridoEm`.
- `OrdemServicoFinalizada` — payload: `OrdemServicoId`, `ClienteId`, `OcorridoEm`.

Ports de ACL (interfaces no vocabulário deste módulo): `IClienteInfoPort`, `IVeiculoInfoPort`, `IServicoInfoPort`, `IPecaDisponibilidadePort`.

Interfaces: `IOrdemServicoRepository`.

**Validação:** Projeto compila. Transições inválidas lançam erro de domínio. Referências de projeto limitadas a `SharedKernel.Domain`.

---

### T21 — OrdemServico.Contracts

- `IOrdemServicoResumoQuery` — `ObterPorId(Guid)` retornando `OrdemServicoResumoDto`
- `IListarOrdensPorClienteQuery` — `Listar(Guid clienteId)` retornando `IReadOnlyList<OrdemServicoResumoDto>`
- DTOs: `OrdemServicoResumoDto`, `OrcamentoDto`, `ItemServicoDto`, `ItemPecaDto`
- DTO de evento: `ItemPecaEventDto(PecaInsumoId, Quantidade)`
- Integration events:
  - `OrcamentoGeradoIntegrationEvent(EventId, OcorridoEm, OrdemServicoId, OrcamentoId, IReadOnlyList<ItemPecaEventDto> Pecas)` — consumido por PecasInsumos para decrementar estoque.
  - `OrcamentoRejeitadoIntegrationEvent(EventId, OcorridoEm, OrdemServicoId, OrcamentoId, IReadOnlyList<ItemPecaEventDto> Pecas)` — consumido por PecasInsumos para estornar estoque.

**Validação:** Projeto compila. Única referência de projeto é `SharedKernel.Domain`.

---

### T22 — OrdemServico.Application

Implementar os commands do agregado, um por pasta:

| Command | O que faz |
|---|---|
| `GerarOrdemServico` | Valida que cliente e veículo existem via ACL; cria a OS com status `Recebida` |
| `IniciarDiagnostico` | Muda status para `EmDiagnostico` |
| `RegistrarDiagnostico` | Verifica disponibilidade de todas as peças via ACL (`IPecaDisponibilidadePort`); chama `aggregate.RegistrarDiagnostico(desc, servicos, pecas)` — que registra itens e cria orçamento com `Status = Pendente`; OS permanece em `EmDiagnostico`. A transição para `AguardandoAprovacao` ocorre no handler `EnviarOrcamentoAoCliente` |
| `AprovarOrcamento` | Muda status do orçamento para `Aprovado` |
| `RejeitarOrcamento` | Muda status do orçamento para `Rejeitado` |
| `ExecutarOrdemServico` | Muda status da OS para `EmExecucao` |
| `FinalizarOrdemServico` | Muda status para `Finalizada` |
| `NotificarCliente` | Preenche `notificado_em` |
| `ConcluirOrdemServico` | Muda status para `Entregue`; preenche `entregue_em` |

Queries: `ObterOrdemServicoPorId`, `ListarOrdensPorCliente`.

**Domain event handlers** (implementam `INotificationHandler<>`, ficam na pasta `DomainEventHandlers/`):

| Handler | Reage a | O que faz |
|---|---|---|
| `DiagnosticoConcluidoHandler` | `DiagnosticoConcluido` | Enfileira `OrcamentoGeradoIntegrationEvent` em `IPendingIntegrationEvents` |
| `OrcamentoRejeitadoHandler` | `OrcamentoRejeitado` | Enfileira `OrcamentoRejeitadoIntegrationEvent` em `IPendingIntegrationEvents` |
| `OrdemServicoFinalizadaHandler` | `OrdemServicoFinalizada` | Loga notificação ao cliente (stub — implementação real na fase de notificações) |

> **Por que handlers de domain event e não o command handler diretamente:** o command handler não deve saber quais integration events publicar — isso é responsabilidade de quem reage ao fato de domínio. Separar em handlers de domain event mantém o command handler focado em orquestração e isola a lógica de cross-BC communication.

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
- `PATCH /ordens-servico/{id}/iniciar-diagnostico`
- `PATCH /ordens-servico/{id}/registrar-diagnostico` (body com descrição + lista de serviços + lista de peças)
- `PATCH /ordens-servico/{id}/aprovar-orcamento`
- `PATCH /ordens-servico/{id}/rejeitar-orcamento`
- `PATCH /ordens-servico/{id}/executar`
- `PATCH /ordens-servico/{id}/finalizar`
- `PATCH /ordens-servico/{id}/notificar-cliente`
- `PATCH /ordens-servico/{id}/concluir`
- `GET /ordens-servico/{id}`
- `GET /ordens-servico?clienteId={id}`

**Validação:** Todos os endpoints aparecem no Swagger. Fluxo completo de uma OS (criar → concluir) funciona via chamadas sequenciais no Swagger UI.

---

### T25 — Testes — OrdemServico

- `OrdemServico.Domain.Tests`: cada transição de estado válida e inválida; `RegistrarDiagnostico` com itens válidos → emite `DiagnosticoConcluido` com payload correto (lista de itens, valorTotal, orcamentoId); `RejeitarOrcamento` → emite `OrcamentoRejeitado` com lista de peças; `Finalizar` → emite `OrdemServicoFinalizada` com `ClienteId`; cálculo de valor total do orçamento; regra de snapshot de preço nos itens.
- `OrdemServico.Application.Tests`: handlers com mocks das ports ACL e repositórios; `DiagnosticoConcluidoHandler` enfileira `OrcamentoGeradoIntegrationEvent`; `OrcamentoRejeitadoHandler` enfileira `OrcamentoRejeitadoIntegrationEvent`.

**Validação:** `dotnet test` passa 100%. Cobertura de `OrdemServico.Domain` >= 80%.

---

### T26 — Integrar OrdemServico no Bootstrap

`AddOrdemServicoModule` + `AddApplicationPart` no `Program.cs`.

**Validação:** `docker compose up`. Fluxo completo de uma OS funciona do início (`POST /ordens-servico`) até o fim (`PATCH /concluir`).

---

## Fase 5 — Integration Events

### T27 — OrcamentoGerado/Rejeitado → Estoque

Implementar dois handlers em `PecasInsumos.Application/IntegrationEventHandlers/`:

- `DecrementarEstoqueQuandoOrcamentoGerado` — implementa `IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent>`, itera as peças do evento e chama `DecrementarEstoque` para cada uma.
- `IncrementarEstoqueQuandoOrcamentoRejeitado` — implementa `IIntegrationEventHandler<OrcamentoRejeitadoIntegrationEvent>`, itera as peças do evento e chama `IncrementarEstoque` para cada uma (estorno).

Registrar ambos no `AddPecasInsumosModule`. Os eventos são publicados pelo `DiagnosticoConcluidoHandler` e `OrcamentoRejeitadoHandler` em `OrdemServico.Application` (T22).

**Validação:** Concluir diagnóstico com peças decrementa o `quantidade_estoque` em `pecas_insumos.peca_insumo`. Rejeitar o orçamento estorna as quantidades. Verificar diretamente no banco.

---

### T28 — Notificação de OS Finalizada (stub)

O `OrdemServicoFinalizadaHandler` (em `OrdemServico.Application/DomainEventHandlers/`) já reage ao domain event `OrdemServicoFinalizada` emitido pelo agregado. Nesta tarefa, implementar o corpo do handler como stub: apenas logar `"OS {id} finalizada — notificar cliente {clienteId}"`.

Não há integration event para este fluxo: o módulo de notificação futuro será implementado dentro de `OrdemServico` (sem cruzar BC). Quando existir o serviço de notificação real, bastará substituir o log pelo envio efetivo dentro do próprio handler.

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
