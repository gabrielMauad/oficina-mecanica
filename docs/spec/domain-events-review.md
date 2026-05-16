# Spec — Refatoração dos Domain Events

> Este documento substitui a versão anterior, escrita em 2026-05-10. A versão anterior
> identificou corretamente que os domain events do projeto não eram consumidos em runtime,
> mas propunha preservar a maior parte deles como "fato-de-domínio sem handler" — solução
> que carrega adiante o problema diagnosticado. Esta revisão aplica filosofia de DDD real
> ao problema e redefine a refatoração.
>
> _Última atualização: 2026-05-11._

---

## 1. Filosofia adotada

Este repositório serve como projeto pessoal de estudo de DDD, CQRS e arquitetura de
microsserviços. Embora seja um MVP de Tech Challenge, **a meta é seguir prática real
de DDD** — não usar o rótulo "MVP" como atalho para arquitetura simplificada.

Consequências:

- **Eventos órfãos não são aceitáveis.** Em DDD aplicado (Vernon, Evans, Khononov),
  domain events representam fatos de negócio que merecem reação. Emitir um evento que
  ninguém consome ou que existe "para auditoria futura" é anti-padrão consagrado na
  literatura.
- **Auditoria não se faz emitindo records.** Quando aparecer requisito de auditoria, ela
  tem mecanismos próprios (event sourcing dedicado, interceptors EF, change data capture).
  Não justificativa para manter eventos.
- **A granularidade do agregado é decisão de design.** Se o command handler precisa
  chamar 2+ métodos do agregado em sequência sem decisão humana entre eles, o sintoma é
  agregado mal-talhado — não falta de eventos intermediários. O conserto é colapsar a
  operação no agregado.
- **Indireção por evento só justifica quando desacopla decisão real.** Eventos in-process
  que disparam reações síncronas previsíveis (cerimônia para espelhar diagrama de event
  storming) adicionam complexidade sem decoupling.
- **Cross-BC continua sendo a fronteira natural para indireção.** Integration events
  permanecem o mecanismo de desacoplamento entre BCs.

## 2. Critério único de manutenção de evento

Para cada domain event candidate, vale **uma única regra:**

> **Existe um consumidor não-especulativo no projeto, agora?**
>
> - Sim, in-process (`INotificationHandler<T>` que faz algo significativo dentro do mesmo
>   BC) → mantém.
> - Sim, cross-BC (handler interno publica integration event que outro BC consome) →
>   mantém.
> - Não → **remove o evento**. Restaurar quando o consumidor aparecer.

A mesma regra vale para integration events em `<Modulo>.Contracts`.

---

## 3. Decisões de modelagem

### 3.1 Recorte do fluxo diagnóstico → orçamento

**Decisão: operação única no agregado.**

O fluxo do event storming "[CMD Registrar Diagnóstico] → [EV Análise Realizada] →
(cross-BC) → [EV Peças Adicionadas] → [POL Gerar Orçamento Automaticamente] → [CMD Gerar
Orçamento] → [EV Orçamento Gerado]" é, sob a ótica do negócio, **um único ato do
mecânico**. Não há decisão humana entre os passos. Em DDD aplicado, isso é uma única
operação coesa no agregado.

Implementação:

```
método único no agregado OrdemServico:
  RegistrarDiagnostico(string descricao,
                      IEnumerable<ItemServicoInput> servicos,
                      IEnumerable<ItemPecaInput> pecas)
```

O método:
- valida status (`EmDiagnostico`)
- registra a descrição
- adiciona itens de serviço com snapshot de preço
- adiciona itens de peça com snapshot de preço
- cria `Orcamento` com `Status = Pendente`, calculando o valor total
- **OS status permanece em `EmDiagnostico`** (mudança para `AguardandoAprovacao` ocorre
  apenas após o envio efetivo do orçamento — ver 3.2)
- emite **um único** domain event `DiagnosticoConcluido` com payload completo

Os seguintes métodos do agregado **deixam de existir**:
- `AdicionarPecaInsumo`, `RemoverPecaInsumo`, `AtualizarQuantidadePecaInsumo`,
  `AtualizarPrecoUnitarioPecaInsumo`
- `AdicionarServico`, `RemoverServico`, `AtualizarQuantidadeServico`,
  `AtualizarPrecoUnitarioServico`
- `GerarOrcamento()`

Existiam apenas para suportar o design fragmentado.

### 3.2 Envio do orçamento ao cliente: reação a evento

**Decisão: handler in-process reage a `DiagnosticoConcluido`.**

Enviar o orçamento ao cliente é side-effect externo (futuro multicanal — email/SMS/
WhatsApp). Misturar isso na operação atômica acopla o agregado a esse efeito. Solução:

- O agregado deixa o orçamento em status `Pendente` ao final de `RegistrarDiagnostico`.
- Um `INotificationHandler<DiagnosticoConcluido>` reage e:
  - carrega a OS
  - chama `EnviarOrcamento(date)` no agregado — método que muta o orçamento para
    `Enviado`, preenche `data_envio`, e muda OS status para `AguardandoAprovacao`
  - efetua o envio (MVP: log stub; futuro: chamada ao serviço de notificação)
  - **não emite novo evento** (sem consumidor)

Quando o canal real de notificação for implementado, ele substitui apenas este handler.
Agregado, command handler e endpoint não mudam.

### 3.3 Decremento de estoque: na geração do orçamento

**Decisão: decremento no momento em que o orçamento é gerado.**

Justificativa: evita o cenário de o orçamento de um cliente A reservar peça que cliente B
"compra" via aprovação de outro orçamento no intervalo entre geração e aprovação.

Implementação: um segundo `INotificationHandler<DiagnosticoConcluido>` reage e publica
`OrcamentoGeradoIntegrationEvent` (carregando peças) via `IPendingIntegrationEvents`.
`PecasInsumos.Application` consome o integration event e decrementa estoque.

### 3.4 Rejeição: novo evento + reversão de estoque

**Decisão: criar domain event `OrcamentoRejeitado` e integration event correspondente.**

Como o decremento ocorre na geração, rejeitar significa devolver peças ao estoque.

- `OrcamentoRejeitado` é emitido por `RejeitarOrcamento()` no agregado (hoje esse método
  não emite nada). Payload inclui a lista de peças.
- `INotificationHandler<OrcamentoRejeitado>` publica `OrcamentoRejeitadoIntegrationEvent`.
- `PecasInsumos.Application` consome e incrementa estoque para cada peça.

### 3.5 Notificação de finalização: também é reação a evento

**Decisão: handler in-process reage a `OrdemServicoFinalizada`.**

Mesma lógica aplicada ao envio do orçamento. `Finalizar()` no agregado emite
`OrdemServicoFinalizada`. Handler reage:
- carrega a OS
- chama `NotificarCliente(date)` no agregado — método que preenche `notificado_em`
- efetua a notificação (MVP: log stub)
- não emite novo evento

Endpoint `PATCH .../notificar-cliente` deixa de existir.

### 3.6 Limitação consciente: sem timeout

Se o cliente nunca aprovar nem rejeitar, a OS permanece em `AguardandoAprovacao`
indefinidamente e o estoque permanece reservado. **Aceito como limitação do MVP.**
Solução natural (não escopo): scheduler que rejeita orçamentos pendentes há mais de N
dias.

---

## 4. Infraestrutura de despacho

### 4.1 Princípio: post-commit centralizado no TransactionBehavior

O mesmo bug que motivou a criação de `IPendingIntegrationEvents` se aplica a domain
events: efeitos colaterais de um fato só podem ocorrer **depois** que o fato está
persistido. Domain events são despachados **após** `SaveChangesAsync` retornar com
sucesso, dentro do `TransactionBehavior` (não em override no DbContext) — mantém visível
em um único lugar a ordem total das operações.

### 4.2 Mecânica completa

```
TransactionBehavior.Handle:
  1. response = await next()
       ← command handler executa, muta agregado(s),
         agregado(s) acumulam domain events via AddDomainEvent
         NÃO toca IPendingIntegrationEvents

  2. if response.IsFailure → return response

  3. domainEvents = unitOfWork.CollectDomainEvents()
       ← varre ChangeTracker.Entries<IHasDomainEvents>()
         e agrega todos os DomainEvents

  4. await unitOfWork.SaveChangesAsync()       ← COMMIT

  5. unitOfWork.ClearDomainEvents()

  6. foreach (de in domainEvents)
        await publisher.Publish(de)            ← MediatR INotification
       ← handlers podem:
         - mutar outro agregado (causa SaveChanges aninhado)
         - enfileirar integration event via IPendingIntegrationEvents
         - chamar serviço externo (log, email…)

  7. foreach (publish in pendingEvents.GetPending())
        await publish()                        ← integration events vão ao bus
```

### 4.3 Componentes a criar/modificar

**Em `SharedKernel.Domain`:**

```csharp
// Marker passa a herdar INotification para ser dispatchable por IPublisher.
// Dependência leve: MediatR.Contracts (pacote separado e mínimo, ~5KB).
public interface IDomainEvent : INotification;

// Interface não-genérica para ChangeTracker conseguir consultar.
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{ /* implementação atual */ }
```

**Em `SharedKernel.Application`:**

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    IReadOnlyList<IDomainEvent> CollectDomainEvents();
    void ClearDomainEvents();
}
```

`TransactionBehavior` modificado conforme 4.2.

**Em cada `<Modulo>.Infrastructure/<Modulo>DbContext.cs`:**

```csharp
public IReadOnlyList<IDomainEvent> CollectDomainEvents() =>
    ChangeTracker.Entries<IHasDomainEvents>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

public void ClearDomainEvents()
{
    foreach (var entry in ChangeTracker.Entries<IHasDomainEvents>())
        entry.Entity.ClearDomainEvents();
}
```

### 4.4 Handlers que mutam estado

Quando um domain event handler precisa mutar outro agregado (ex.:
`EnviarOrcamentoAoCliente` carrega a OS e chama `EnviarOrcamento(UtcNow)`), o handler
é responsável por chamar `IUnitOfWork.SaveChangesAsync()` ao final — não há SaveChanges
automático após o passo 6 do `TransactionBehavior`. Esse SaveChanges aninhado ocorre em
sua própria unidade transacional do EF Core (após o commit original ter retornado).

Ordem dentro do passo 6: handlers do mesmo evento rodam sequencialmente; cada um
completa (incluindo seu próprio SaveChanges, se houver) antes do próximo iniciar.
Após o passo 6, o passo 7 publica os integration events enfileirados — então, no
momento em que `PecasInsumos` recebe `OrcamentoGeradoIntegrationEvent`, o estado de
`OrdemServico` já reflete o orçamento como `Enviado`.

### 4.5 Modos de falha e trade-offs

- **Commit falha:** `TransactionBehavior` lança/retorna erro antes do passo 6. Nenhum
  evento foi despachado. Consistente.
- **Domain event handler falha (passo 6):** estado original já foi persistido (passo 4).
  Handler não conclui; reações subsequentes (incluindo handlers seguintes e integration
  events que dependiam desse handler ter enfileirado) ficam em débito. Inconsistência
  eventual.
- **Integration event publish falha (passo 7):** commit já ocorreu, fila parcialmente
  despachada.

Esses dois últimos modos são o trade-off de in-memory dispatch. **Aceitos para o MVP.**
Solução de produção: **Outbox Pattern** (persistir os eventos na mesma transação numa
tabela `outbox`, worker assíncrono lê e despacha com retry/idempotência). Registrado
como evolução futura — fora do escopo desta fase.

---

## 5. Inventário final dos eventos

### 5.1 Domain events sobreviventes (3)

| Evento | Onde fica | Quando é emitido | Quem consome |
|---|---|---|---|
| `DiagnosticoConcluido` | `OrdemServico.Domain` | `RegistrarDiagnostico(...)` | 2 handlers in-process: (a) publica `OrcamentoGeradoIntegrationEvent` para `PecasInsumos`; (b) carrega OS e chama `EnviarOrcamento(date)` + efetua envio |
| `OrcamentoRejeitado` | `OrdemServico.Domain` | `RejeitarOrcamento()` | 1 handler in-process: publica `OrcamentoRejeitadoIntegrationEvent` para `PecasInsumos` |
| `OrdemServicoFinalizada` | `OrdemServico.Domain` | `Finalizar()` | 1 handler in-process: carrega OS, chama `NotificarCliente(date)` + efetua notificação |

**Payloads:**

```csharp
public sealed record DiagnosticoConcluido(
    OrdemServicoId OrdemServicoId,
    OrcamentoId OrcamentoId,
    string DescricaoDiagnostico,
    IReadOnlyList<(Guid ServicoId, int Quantidade, decimal PrecoSnapshot)> Servicos,
    IReadOnlyList<(Guid PecaId, int Quantidade, decimal PrecoSnapshot)> Pecas,
    decimal ValorTotal,
    DateTime OcorridoEm
) : IDomainEvent;

public sealed record OrcamentoRejeitado(
    OrdemServicoId OrdemServicoId,
    OrcamentoId OrcamentoId,
    IReadOnlyList<(Guid PecaId, int Quantidade)> Pecas,
    DateTime OcorridoEm
) : IDomainEvent;

public sealed record OrdemServicoFinalizada(
    OrdemServicoId OrdemServicoId,
    Guid ClienteId,
    DateTime OcorridoEm
) : IDomainEvent;
```

### 5.2 Domain events removidos (15)

| Evento | Módulo | Por quê |
|---|---|---|
| `ClienteCadastrado` | Cadastro | Sem consumidor. Outros módulos consultam via ACL síncrona. |
| `VeiculoCadastrado` | Cadastro | Idem. |
| `ServicoCadastrado` | Cadastro | Idem. |
| `PecaInsumoAdicionada` | PecasInsumos | Sem consumidor. |
| `EstoqueAtualizado` | PecasInsumos | Sem consumidor. Ambíguo (emite em increment e decrement). |
| `EstoqueEsgotado` | PecasInsumos | Sem consumidor. Política do event storming "impedir inclusão" já é preventiva via ACL. |
| `OrdemServicoGerada` | OrdemServico | Sem consumidor. Status já reflete o fato. |
| `DiagnosticoIniciado` | OrdemServico | Sem consumidor. Status já reflete. |
| `DiagnosticoRegistrado` | OrdemServico | Substituído por `DiagnosticoConcluido`. |
| `OrcamentoGerado` (atual) | OrdemServico | Substituído por `DiagnosticoConcluido`. |
| `OrcamentoEnviado` | OrdemServico | Sem consumidor. Mutação interna basta. |
| `OrcamentoAprovado` | OrdemServico | Sem decremento (movido para geração) → sem consumidor. |
| `OrdemServicoEmExecucao` | OrdemServico | Sem consumidor. |
| `ClienteNotificado` | OrdemServico | Sem consumidor. Coluna `notificado_em` é estado suficiente. |
| `OrdemServicoConcluida` | OrdemServico | Sem consumidor. |

### 5.3 Integration events em Contracts

**Sobreviventes (2):**

```csharp
// OrdemServico.Contracts
public sealed record OrcamentoGeradoIntegrationEvent(
    Guid EventId,
    DateTime OcorridoEm,
    Guid OrdemServicoId,
    Guid OrcamentoId,
    IReadOnlyList<ItemPecaEventDto> Pecas
) : IIntegrationEvent;

public sealed record OrcamentoRejeitadoIntegrationEvent(
    Guid EventId,
    DateTime OcorridoEm,
    Guid OrdemServicoId,
    Guid OrcamentoId,
    IReadOnlyList<ItemPecaEventDto> Pecas
) : IIntegrationEvent;
```

`ItemPecaEventDto` (já existente) é reutilizado.

**Removidos:**
- `ClienteCadastradoIntegrationEvent` (Cadastro.Contracts) — sem consumidor cross-BC.
- `VeiculoCadastradoIntegrationEvent` (Cadastro.Contracts) — sem consumidor cross-BC.
- `EstoqueDecrementadoIntegrationEvent` (PecasInsumos.Contracts) — sem consumidor cross-BC.
- `OrcamentoAprovadoIntegrationEvent` (OrdemServico.Contracts) — sem consumidor; decremento moveu-se para `OrcamentoGeradoIntegrationEvent`.
- `OrdemServicoFinalizadaIntegrationEvent` (OrdemServico.Contracts) — único consumidor é handler in-process da própria OS, não justifica cross-BC.

### 5.4 Integration event handlers

| Handler | Local | Reage a |
|---|---|---|
| `DecrementarEstoqueQuandoOrcamentoGerado` | `PecasInsumos.Application` | `OrcamentoGeradoIntegrationEvent` |
| `IncrementarEstoqueQuandoOrcamentoRejeitado` | `PecasInsumos.Application` | `OrcamentoRejeitadoIntegrationEvent` |

Ambos registrados em `AddPecasInsumosModule`.

### 5.5 Domain event handlers (in-process)

| Handler | Local | Reage a | O que faz |
|---|---|---|---|
| `PublicarOrcamentoGeradoQuandoDiagnosticoConcluido` | `OrdemServico.Application` | `DiagnosticoConcluido` | Enfileira `OrcamentoGeradoIntegrationEvent` via `IPendingIntegrationEvents` |
| `EnviarOrcamentoAoCliente` | `OrdemServico.Application` | `DiagnosticoConcluido` | Carrega OS, chama `EnviarOrcamento(UtcNow)`, efetua envio (stub log no MVP) |
| `PublicarOrcamentoRejeitado` | `OrdemServico.Application` | `OrcamentoRejeitado` | Enfileira `OrcamentoRejeitadoIntegrationEvent` |
| `NotificarClienteAoFinalizar` | `OrdemServico.Application` | `OrdemServicoFinalizada` | Carrega OS, chama `NotificarCliente(UtcNow)`, efetua notificação (stub log no MVP) |

### 5.6 Agregado OrdemServico — métodos após refatoração

**Mantidos (sem alteração):**
- `Criar(clienteId, veiculoId)` — não emite evento (era `OrdemServicoGerada`)
- `IniciarDiagnostico()` — não emite evento (era `DiagnosticoIniciado`)
- `AprovarOrcamento()` — não emite evento (era `OrcamentoAprovado`)
- `Executar()` — não emite evento (era `OrdemServicoEmExecucao`)
- `Concluir(date)` — não emite evento (era `OrdemServicoConcluida`)

**Mantidos (mas com comportamento adaptado):**
- `EnviarOrcamento(date)` — sem evento. Chamado pelo handler `EnviarOrcamentoAoCliente`.
- `NotificarCliente(date)` — sem evento. Chamado pelo handler `NotificarClienteAoFinalizar`.
- `RejeitarOrcamento()` — **passa a emitir** `OrcamentoRejeitado`.
- `Finalizar()` — continua emitindo `OrdemServicoFinalizada`.

**Assinatura mudada:**
- `RegistrarDiagnostico(string, IEnumerable<ItemServicoInput>, IEnumerable<ItemPecaInput>)` — emite `DiagnosticoConcluido`.

**Removidos:**
- `AdicionarPecaInsumo`, `RemoverPecaInsumo`, `AtualizarQuantidadePecaInsumo`, `AtualizarPrecoUnitarioPecaInsumo`
- `AdicionarServico`, `RemoverServico`, `AtualizarQuantidadeServico`, `AtualizarPrecoUnitarioServico`
- `GerarOrcamento()`

### 5.7 Endpoints REST (T24)

**Eliminados** (operações tornadas internas/automáticas):
- `PATCH /ordens-servico/{id}/gerar-orcamento`
- `PATCH /ordens-servico/{id}/enviar-orcamento`
- `PATCH /ordens-servico/{id}/notificar-cliente`

**Mantidos:**
- `POST /ordens-servico` — criar OS
- `PATCH /ordens-servico/{id}/iniciar-diagnostico`
- `PATCH /ordens-servico/{id}/registrar-diagnostico` — body rico (descrição + serviços + peças)
- `PATCH /ordens-servico/{id}/aprovar-orcamento`
- `PATCH /ordens-servico/{id}/rejeitar-orcamento`
- `PATCH /ordens-servico/{id}/executar`
- `PATCH /ordens-servico/{id}/finalizar`
- `PATCH /ordens-servico/{id}/concluir`
- `GET /ordens-servico/{id}`
- `GET /ordens-servico?clienteId={id}`

---

## 6. Fluxo end-to-end (happy path)

```
1. POST /ordens-servico
   → GerarOrdemServicoCommand
     → OrdemServico.Criar(clienteId, veiculoId)
     → SaveChanges (commit)
     → nenhum domain event emitido, nenhum integration event

2. PATCH /{id}/iniciar-diagnostico
   → IniciarDiagnosticoCommand
     → os.IniciarDiagnostico() → Status = EmDiagnostico
     → SaveChanges
     → nenhum evento

3. PATCH /{id}/registrar-diagnostico  { descricao, servicos[], pecas[] }
   → RegistrarDiagnosticoCommand
     → valida disponibilidade de peças via IPecaDisponibilidadePort
     → valida existência de serviços via IServicoInfoPort
     → os.RegistrarDiagnostico(desc, servicos, pecas)
        - adiciona itens com snapshot
        - cria Orcamento status=Pendente
        - emite DiagnosticoConcluido
     → SaveChanges (commit)
     → Publish(DiagnosticoConcluido)
        ├─ handler 1: enqueue OrcamentoGeradoIntegrationEvent
        └─ handler 2: load OS, os.EnviarOrcamento(UtcNow), log "orçamento enviado"
                      → SaveChanges (aninhado, persiste a mudança do envio)
     → pendingEvents.GetPending() → publish OrcamentoGeradoIntegrationEvent
        └─ DecrementarEstoqueQuandoOrcamentoGerado consome → decrementa estoque

4. PATCH /{id}/aprovar-orcamento  (ou /rejeitar-orcamento)

   Aprovação:
     → AprovarOrcamentoCommand
       → os.AprovarOrcamento() → orçamento Status = Aprovado
       → SaveChanges
       → nenhum evento

   Rejeição:
     → RejeitarOrcamentoCommand
       → os.RejeitarOrcamento() → orçamento Status = Rejeitado
         → emite OrcamentoRejeitado
       → SaveChanges
       → Publish(OrcamentoRejeitado)
          └─ enqueue OrcamentoRejeitadoIntegrationEvent
       → publish OrcamentoRejeitadoIntegrationEvent
          └─ IncrementarEstoqueQuandoOrcamentoRejeitado consome → devolve estoque

5. PATCH /{id}/executar
   → ExecutarOrdemServicoCommand → os.Executar() → Status = EmExecucao
   → nenhum evento

6. PATCH /{id}/finalizar
   → FinalizarOrdemServicoCommand
     → os.Finalizar() → Status = Finalizada → emite OrdemServicoFinalizada
     → SaveChanges
     → Publish(OrdemServicoFinalizada)
        └─ NotificarClienteAoFinalizar: load OS, os.NotificarCliente(UtcNow), log
           → SaveChanges (aninhado, persiste notificado_em)

7. PATCH /{id}/concluir
   → ConcluirOrdemServicoCommand → os.Concluir(date) → Status = Entregue
   → nenhum evento
```

---

## 7. Impacto no plano de tarefas

### T05 — Cadastro.Domain
- Remover domain event `ClienteCadastrado` e sua emissão em `Cliente.Criar()`.
- Remover domain event `VeiculoCadastrado` e sua emissão em `Veiculo.Criar()`.
- Remover domain event `ServicoCadastrado` e sua emissão em `Servico.Criar()`.

### T06 — Cadastro.Contracts
- Remover `ClienteCadastradoIntegrationEvent`.
- Remover `VeiculoCadastradoIntegrationEvent`.

### T07 — Cadastro.Application
- `CadastrarClienteHandler` para de injetar e usar `IIntegrationEventBus` e
  `IPendingIntegrationEvents`. Apenas persiste.
- Idem para `CadastrarVeiculoHandler` e `AdicionarServicoHandler`.

### T13 — PecasInsumos.Domain
- Remover domain events `PecaInsumoAdicionada`, `EstoqueAtualizado`, `EstoqueEsgotado` e
  suas emissões.

### T14 — PecasInsumos.Contracts
- Remover `EstoqueDecrementadoIntegrationEvent`.

### T15 — PecasInsumos.Application
- `DecrementarEstoqueHandler` para de injetar `IIntegrationEventBus` e
  `IPendingIntegrationEvents`. Apenas persiste.
- Criar `IntegrationEventHandlers/DecrementarEstoqueQuandoOrcamentoGerado`
  (consome `OrcamentoGeradoIntegrationEvent` — disponível após T21 revisada).
- Criar `IntegrationEventHandlers/IncrementarEstoqueQuandoOrcamentoRejeitado`.
- A nota da T15 original ("estoque verificado em RegistrarDiagnostico via ACL sem
  decremento, decrementado na aprovação") é substituída por: estoque verificado em
  RegistrarDiagnostico via ACL, decrementado via integration event ao gerar o orçamento.

### T20 — OrdemServico.Domain (REVISÃO MAIOR)
- Remover domain events: `OrdemServicoGerada`, `DiagnosticoIniciado`,
  `DiagnosticoRegistrado` (atual), `OrcamentoGerado` (atual), `OrcamentoEnviado`,
  `OrcamentoAprovado`, `OrdemServicoEmExecucao`, `ClienteNotificado`,
  `OrdemServicoConcluida`.
- Criar domain events `DiagnosticoConcluido` e `OrcamentoRejeitado` (payloads em 5.1).
- Manter `OrdemServicoFinalizada` (ajustar payload para incluir `ClienteId` se ainda
  não inclui).
- Alterar assinatura de `RegistrarDiagnostico` para receber descrição + serviços + peças;
  método passa a criar `Orcamento(Pendente)` internamente e emitir `DiagnosticoConcluido`.
- Remover métodos: `AdicionarPecaInsumo`, `RemoverPecaInsumo`,
  `AtualizarQuantidadePecaInsumo`, `AtualizarPrecoUnitarioPecaInsumo`, `AdicionarServico`,
  `RemoverServico`, `AtualizarQuantidadeServico`, `AtualizarPrecoUnitarioServico`,
  `GerarOrcamento`.
- `RejeitarOrcamento()` passa a emitir `OrcamentoRejeitado`.
- `Criar`, `IniciarDiagnostico`, `EnviarOrcamento`, `AprovarOrcamento`, `Executar`,
  `Finalizar`, `NotificarCliente`, `Concluir` continuam funcionando, mas sem emitir
  eventos (exceto `Finalizar`).

### T21 — OrdemServico.Contracts
- Adicionar `OrcamentoGeradoIntegrationEvent` (payload em 5.3).
- Adicionar `OrcamentoRejeitadoIntegrationEvent` (payload em 5.3).
- Remover `OrcamentoAprovadoIntegrationEvent`.
- Remover `OrdemServicoFinalizadaIntegrationEvent`.
- Manter `ItemPecaEventDto` (reutilizado).

### T22 — OrdemServico.Application
- `RegistrarDiagnosticoCommand` recebe descrição + serviços + peças. Handler valida tudo
  via ACL (peças via `IPecaDisponibilidadePort`, serviços via `IServicoInfoPort`); se
  qualquer validação falhar, retorna erro sem persistir. Chama
  `os.RegistrarDiagnostico(desc, servicos, pecas)`.
- Commands eliminados: `GerarOrcamentoCommand`, `EnviarOrcamentoCommand` (público),
  `NotificarClienteCommand` (público).
- Domain event handlers a criar em `OrdemServico.Application/DomainEventHandlers/`:
  - `PublicarOrcamentoGeradoQuandoDiagnosticoConcluido`
  - `EnviarOrcamentoAoCliente`
  - `PublicarOrcamentoRejeitado`
  - `NotificarClienteAoFinalizar`
- `AprovarOrcamentoCommand` e `FinalizarOrdemServicoCommand` param de enfileirar
  integration events diretamente (a versão anterior do plano dizia que esse era o
  comportamento esperado).

### T24 — OrdemServico.Presentation
- Endpoints eliminados:
  - `PATCH /ordens-servico/{id}/gerar-orcamento`
  - `PATCH /ordens-servico/{id}/enviar-orcamento`
  - `PATCH /ordens-servico/{id}/notificar-cliente`
- Endpoint `PATCH /ordens-servico/{id}/registrar-diagnostico` recebe body rico (DTOs de
  serviços e peças).

### T27 — Reescrita: "Gestão de Estoque por Orçamento"
- Substitui o nome original "OrcamentoAprovado → DecrementarEstoque".
- Implementa `DecrementarEstoqueQuandoOrcamentoGerado` e
  `IncrementarEstoqueQuandoOrcamentoRejeitado` em `PecasInsumos.Application`.
- Registra ambos em `AddPecasInsumosModule`.

### T28 — Absorvida por T22
- O comportamento que T28 descreve (stub de notificação ao finalizar) passa a ser o
  handler `NotificarClienteAoFinalizar` em T22.

### Nova tarefa: T-INFRA — Infraestrutura de despacho de domain events
Deve ser executada **antes** de T22 (e antes de qualquer refatoração que dependa dela).

1. `SharedKernel.Domain`:
   - Adicionar dependência ao pacote `MediatR.Contracts`.
   - `IDomainEvent : INotification`.
   - Criar interface `IHasDomainEvents`.
   - `AggregateRoot<TId>` implementa `IHasDomainEvents`.
2. `SharedKernel.Application`:
   - Estender `IUnitOfWork` com `CollectDomainEvents()` e `ClearDomainEvents()`.
   - Modificar `TransactionBehavior` conforme 4.2.
3. Cada `<Modulo>.Infrastructure/<Modulo>DbContext`:
   - Implementar `CollectDomainEvents()` e `ClearDomainEvents()` via `ChangeTracker`.

---

## 8. O que NÃO muda

- A separação entre domain events (`IDomainEvent`) e integration events
  (`IIntegrationEvent`) permanece intacta e correta.
- `IPendingIntegrationEvents` continua existindo e sendo o canal de saída dos integration
  events. Apenas muda quem enfileira (domain event handlers, não mais command handlers).
- `TransactionBehavior` continua publicando os pending integration events após o commit
  (passo 7 em 4.2). Nenhuma alteração nessa parte do fluxo.
- A regra "nenhum domain event cruza fronteira de BC" permanece absoluta.
- A regra "nenhuma FK cross-schema no banco" permanece.
- Os contratos de Query (`ICadastroClienteQuery`, `IPecasInsumosDisponibilidadeQuery`,
  etc.) continuam servindo a ACL síncrona.

---

## 9. Testes

A refatoração permite testes mais focados, não menos:

**Domain.Tests:**
- `OrdemServico.RegistrarDiagnostico` com dados válidos → adiciona itens, cria orçamento
  com status `Pendente`, emite UM `DiagnosticoConcluido`.
- `OrdemServico.RegistrarDiagnostico` em status inválido → erro.
- `OrdemServico.RejeitarOrcamento` → emite `OrcamentoRejeitado` com peças.
- `OrdemServico.Finalizar` → emite `OrdemServicoFinalizada`.
- Demais transições continuam testadas pelo efeito de estado (Status, datas, etc.) — sem
  precisar testar emissão de evento que não existe mais.

**Application.Tests:**
- `RegistrarDiagnosticoHandler` chama os ports ACL e o método do agregado corretamente.
- Domain event handlers: cada um isolado, testado contra payload de evento.
- Integration event handlers em PecasInsumos: consomem evento, atualizam estoque.

**IntegrationTests:**
- Fluxo completo: criar OS → registrar diagnóstico → verificar que estoque foi
  decrementado (via integration event), que orçamento ficou em `Enviado`, que OS está
  em `AguardandoAprovacao`.
- Fluxo de rejeição: registrar diagnóstico → rejeitar → verificar que estoque foi
  devolvido.
- Fluxo de finalização: → verificar que `notificado_em` foi preenchido.

---

## 10. Evolução futura (fora do escopo)

- **Outbox Pattern** para garantir entrega exatamente-uma-vez quando o monolito virar
  microsserviços. Envolve persistir os eventos na mesma transação e um worker assíncrono
  consumindo de forma idempotente.
- **Saga/Process Manager** para o caso de "cliente nunca decide": scheduler que rejeita
  orçamentos pendentes há mais de N dias.
- **Serviço real de notificação** (email/SMS): substitui apenas os handlers
  `EnviarOrcamentoAoCliente` e `NotificarClienteAoFinalizar`. Agregado, command handlers
  e endpoints permanecem inalterados.
- **Restaurar eventos quando consumidor aparecer**: novo BC Financeiro pode demandar
  `OrcamentoAprovado`; novo BC Analytics pode demandar `OrdemServicoConcluida`. Quando
  isso acontecer, restaura-se o evento + integration event + handler, na mesma sessão
  em que se justifica a necessidade.

---

_Spec gerada em 2026-05-11 substituindo a versão de 2026-05-10. Validada contra o estado
real do código em `src/Modules/`, o event storming em
`docs/spec/event-storming-contextos-delimitados.md`, e a arquitetura em
`docs/arquitetura/estrutura-do-projeto.md`._
