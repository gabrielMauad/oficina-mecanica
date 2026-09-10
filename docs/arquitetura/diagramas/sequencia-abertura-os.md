# Desenho — Sequência de Abertura de Ordem de Serviço

> Diagrama de sequência do fluxo de **abertura de uma OS**, item obrigatório da Fase 3. Reflete o
> código como ele existe hoje — não um desenho idealizado. Fonte de verdade:
> [`OrdemServicoApiController`](../../../src/Modules/OrdensServico/OrdensServico.Web/Controllers/OrdemServicoApiController.cs),
> [`OrdemServicoController`](../../../src/Modules/OrdensServico/OrdensServico.Adapters/Controllers/OrdemServicoController.cs),
> os handlers `GerarOrdemServico` / `AbrirOrdemServicoCompleta`, os Gateways de ACL em
> `OrdensServico.Adapters/Gateways/`, os *behaviors* em `SharedKernel.Application/Behaviors/` e os
> domain/integration events do módulo. Ver também [`clean-architecture.md`](../clean-architecture.md)
> (vocabulário Controller CA / Gateway / Presenter), [`decisoes.md`](../decisoes.md) (ciclo de vida
> da OS e regra de comunicação entre módulos) e [`metricas.md`](../metricas.md) (instrumento
> `oficina.ordens_servico.abertas`).

**Ator:** a **oficina** (atendente/mecânico), autenticada com token de papel `Oficina`
(`[Authorize(Roles = "Oficina")]` nas duas rotas). O cliente não abre OS — ele só participa mais
adiante, aprovando ou rejeitando o orçamento.

**Duas rotas de abertura**, convergindo para o mesmo pipeline:

| Rota | Command | O que já entra pronto |
|---|---|---|
| `POST /api/v1/ordens-servico` | `GerarOrdemServicoCommand` | Nada — a OS nasce em `Recebida`, sem itens, sem orçamento. Diagnóstico vem depois. |
| `POST /api/v1/ordens-servico/completa` | `AbrirOrdemServicoCompletaCommand` | Serviços e peças já informados — a OS nasce direto em `AguardandoAprovacao`, com orçamento gerado, enviado e estoque reservado. |

---

## Diagrama 1 — Caminho feliz (as duas rotas)

```mermaid
sequenceDiagram
    autonumber
    actor Oficina
    participant Web as OrdemServicoApiController<br/>(Web)
    participant CA as OrdemServicoController<br/>(Adapters — Controller CA)
    participant Log as LoggingBehavior
    participant Val as ValidationBehavior
    participant Tx as TransactionBehavior
    participant H as Gerar/AbrirCompleta<br/>Handler (Application)
    participant ClienteACL as ClienteGateway<br/>(ACL → Cadastro)
    participant VeiculoACL as VeiculoGateway<br/>(ACL → Cadastro)
    participant ServicoACL as ServicoGateway<br/>(ACL → Cadastro)
    participant PecaACL as PecaDisponibilidadeGateway<br/>(ACL → PecasInsumos)
    participant OSGW as OrdemServicoGateway
    participant DB as PostgreSQL<br/>(schema ordens_servico)
    participant Pub as IPublisher<br/>(domain events)
    participant Bus as IIntegrationEventBus
    participant Estoque as PecasInsumos<br/>DecrementarEstoqueQuandoOrcamentoGerado
    participant Metrics as OrdensServicoMetrics

    Oficina->>Web: POST /ordens-servico [ou /completa]<br/>Authorize Roles=Oficina
    Web->>CA: Gerar(request) / AbrirCompleta(request)
    CA->>Log: ISender.Send(command)
    activate Log
    Log->>Val: next()
    activate Val
    Val->>Val: FluentValidation<br/>(ClienteId/VeiculoId, [+ Servicos/Pecas na completa])
    Val->>Tx: next() — válido
    activate Tx
    Tx->>H: next()
    activate H

    H->>ClienteACL: ExisteEAtivo(clienteId)
    ClienteACL->>ClienteACL: ICadastroClienteQuery.ObterPorId<br/>(Cadastro.Contracts)
    ClienteACL-->>H: true

    H->>VeiculoACL: ExisteEPertenceAoCliente(veiculoId, clienteId)
    VeiculoACL->>VeiculoACL: ICadastroVeiculoQuery.ObterPorId<br/>(Cadastro.Contracts)
    VeiculoACL-->>H: true

    rect rgb(235, 245, 255)
    Note over H,PecaACL: Rota completa apenas
    loop cada serviço do pedido
        H->>ServicoACL: ObterPreco(servicoId)
        ServicoACL-->>H: preço base (snapshot)
    end
    loop cada peça do pedido
        H->>PecaACL: Verificar(pecaId, quantidade)
        PecaACL->>PecaACL: IPecasInsumosDisponibilidadeQuery<br/>.VerificarDisponibilidade (PecasInsumos.Contracts)
        PecaACL-->>H: disponível + preço unitário (snapshot)
    end
    end

    H->>H: OrdemServico.Criar(...)<br/>ou AbrirComServicos(...) — regra no agregado
    Note right of H: AbrirComServicos monta o Orçamento e levanta<br/>o domain event OrcamentoGerado dentro do agregado
    H->>OSGW: Adicionar(ordemServico)
    OSGW->>OSGW: IDomainEventCollector.Registrar(agregado)
    OSGW->>DB: mapeia p/ Record (EF) — apenas rastreado,<br/>SaveChanges ainda não ocorreu
    H->>Metrics: RegistrarOrdemAberta(tipo: simples|completa)
    H-->>Tx: Result&lt;OrdemServico&gt; sucesso
    deactivate H

    Tx->>Tx: domainEvents = collector.Coletar()
    Tx->>DB: SaveChangesAsync — commit
    DB-->>Tx: ok
    Tx->>Tx: collector.Limpar()

    rect rgb(235, 245, 255)
    Note over Tx,Estoque: Só ocorre na rota completa<br/>(único caminho que já levanta OrcamentoGerado na abertura)
    Tx->>Pub: Publish(OrcamentoGerado)
    Pub->>Pub: PublicarOrcamentoGerado.Handle
    Pub->>Tx: IPendingIntegrationEvents.Enqueue<br/>(OrcamentoGeradoIntegrationEvent)
    Pub->>Pub: EnviarOrcamentoAoCliente.Handle
    Pub->>OSGW: ObterPorId + EnviarOrcamento() + Atualizar()
    Pub->>DB: SaveChangesAsync — commit próprio<br/>(fora da UoW do TransactionBehavior)
    Note right of Pub: Consulta ClienteGateway, VeiculoGateway,<br/>ServicoGateway e PecaInsumoInfoGateway (ACL)<br/>só para montar o corpo do e-mail
    Pub->>Pub: NotificacaoClienteGateway<br/>.NotificarOrcamentoPronto (stub de log)
    Tx->>Bus: Publish(OrcamentoGeradoIntegrationEvent)
    Bus->>Estoque: Handle → DecrementarEstoqueCommand<br/>por peça (módulo PecasInsumos, via ISender)
    end

    Tx-->>Val: Result sucesso
    deactivate Tx
    Val-->>Log: Result sucesso
    deactivate Val
    Log-->>CA: Result&lt;OrdemServico&gt; sucesso
    deactivate Log
    CA->>CA: OrdemServicoPresenter.Present(entidade) → ViewModel
    CA-->>Web: Result&lt;OrdemServicoViewModel&gt;
    Web-->>Oficina: 201 Created + Location: /ordens-servico/{id}
```

---

## Diagrama 2 — Caminhos de erro

As regras de negócio mais interessantes do fluxo estão nas falhas, não no caminho feliz: é aqui
que a ACL protege o agregado de nascer em estado inconsistente (cliente inexistente, veículo de
outro dono, serviço ou peça que não existem mais no catálogo, peça sem estoque).

```mermaid
sequenceDiagram
    autonumber
    actor Oficina
    participant Web as OrdemServicoApiController
    participant CA as OrdemServicoController
    participant Val as ValidationBehavior
    participant Tx as TransactionBehavior
    participant H as Handler
    participant ClienteACL as ClienteGateway
    participant VeiculoACL as VeiculoGateway
    participant ServicoACL as ServicoGateway
    participant PecaACL as PecaDisponibilidadeGateway

    Oficina->>Web: POST /ordens-servico [ou /completa]

    rect rgb(255, 235, 235)
    Note over Web,Val: (a) Falha de validação de formato — nem chega ao Handler
    Web->>CA: Gerar(request) / AbrirCompleta(request)
    CA->>Val: ISender.Send(command)
    Val->>Val: FluentValidation falha<br/>(ex.: ClienteId vazio, Servicos/Pecas vazios,<br/>quantidade &lt;= 0)
    Val-->>CA: Result.Failure(Error.Validation<br/>("validation.failed", ...))
    Note right of Val: Handler e TransactionBehavior nunca são chamados —<br/>curto-circuito dentro do próprio behavior
    CA-->>Web: Result.Failure
    Web-->>Oficina: 422 Unprocessable Entity
    end

    rect rgb(255, 235, 235)
    Note over Web,VeiculoACL: (b) Falha de regra de negócio — ACL responde "não"
    Web->>CA: Gerar(request) / AbrirCompleta(request)
    CA->>Tx: ISender.Send(command) [validação passou]
    Tx->>H: next()
    H->>ClienteACL: ExisteEAtivo(clienteId)
    ClienteACL-->>H: false (inexistente ou inativo)
    H-->>Tx: Result.Failure(OrdemServicoErrors.ClienteInexistenteOuInativo)
    Note right of H: Retorna imediatamente — VeiculoGateway,<br/>ServicoGateway, PecaGateway e OrdemServicoGateway<br/>nunca são chamados
    Tx->>Tx: response is IResult { IsFailure: true }<br/>→ pula coleta de eventos e SaveChanges
    Note right of Tx: Nada é persistido, nenhum domain/integration event<br/>é publicado, e OrdensServicoMetrics.RegistrarOrdemAberta<br/>não é chamado
    Tx-->>CA: Result.Failure
    CA-->>Web: Result.Failure
    Web-->>Oficina: 422 Unprocessable Entity
    end

    rect rgb(255, 235, 235)
    Note over H,VeiculoACL: (c) Mesma forma para veículo de outro dono
    H->>ClienteACL: ExisteEAtivo(clienteId)
    ClienteACL-->>H: true
    H->>VeiculoACL: ExisteEPertenceAoCliente(veiculoId, clienteId)
    VeiculoACL-->>H: false
    H-->>Tx: Result.Failure(OrdemServicoErrors.VeiculoInexistenteOuNaoPertenceAoCliente)
    end

    rect rgb(255, 235, 235)
    Note over H,PecaACL: (d) Rota completa — catálogo ou estoque reprovam o pedido
    H->>ClienteACL: ExisteEAtivo(clienteId)
    ClienteACL-->>H: true
    H->>VeiculoACL: ExisteEPertenceAoCliente(veiculoId, clienteId)
    VeiculoACL-->>H: true
    H->>ServicoACL: ObterPreco(servicoId)
    ServicoACL-->>H: null (serviço não existe mais no catálogo)
    H-->>Tx: Result.Failure(OrdemServicoErrors.ServicoNaoEncontrado)
    Note right of H: Se o serviço passar mas a peça falhar,<br/>o motivo muda para PecaNaoEncontrada (dto nulo)<br/>ou PecaIndisponivel (dto.Disponivel == false)
    end
```

---

## O que os diagramas mostram

**Duas rotas, um único pipeline.** `POST /ordens-servico` (simples) e `POST /ordens-servico/completa`
chegam ao mesmo `OrdemServicoApiController`, ao mesmo `OrdemServicoController` (Controller CA) e ao
mesmo pipeline MediatR — só o Command e o Handler mudam. A diferença de negócio é que a rota
completa já recebe serviços e peças, então o agregado nasce com orçamento gerado (evento
`OrcamentoGerado`, status `AguardandoAprovacao`); a rota simples nasce vazia em `Recebida`, sem
levantar nenhum domain event — o orçamento só existe quando `RegistrarDiagnostico` for chamado
depois.

**O pipeline do MediatR envolve o Handler em camadas.** A ordem de registro em cada
`*Module.cs` é `Logging → Validation → Transaction`, e cada behavior chama `next()` para entrar na
camada de dentro: `LoggingBehavior` mede o tempo total; `ValidationBehavior` roda o
`FluentValidation` do Command **antes** de deixar a requisição prosseguir e curto-circuita com
`Result.Failure` sem nunca invocar o Handler; `TransactionBehavior` só persiste e só publica
eventos **depois** que o Handler retorna sucesso — se o `Result` vier `IsFailure`, ele pula
`SaveChangesAsync` e a publicação de eventos inteiramente (ver Diagrama 2, caminho b).

**Por que a comunicação com Cadastro e PecasInsumos passa por Gateway/ACL, e não por acesso
direto.** `OrdensServico.Application`/`Adapters` nunca referenciam `Cadastro.Domain`,
`Cadastro.Application`, `PecasInsumos.Domain` ou `PecasInsumos.Application` — a Regra de
Dependência da Clean Architecture proíbe um módulo de enxergar o interior de outro bounded
context. Em vez disso, o Handler depende de uma *port* no vocabulário do próprio módulo
(`IClienteGateway`, `IVeiculoGateway`, `IServicoGateway`, `IPecaDisponibilidadeGateway`, todas em
`OrdensServico.Application/Gateways`), e quem implementa essa port é um *adapter* em
`OrdensServico.Adapters/Gateways` que traduz o contrato público do módulo produtor
(`Cadastro.Contracts.Queries.*`, `PecasInsumos.Contracts.Queries.*`) para o formato que
OrdensServico entende. Isso é o Anti-Corruption Layer descrito em
[`clean-architecture.md`](../clean-architecture.md) §4.3: se o schema interno de Cadastro mudar,
só o adapter muda — o Handler e o Domain de OrdensServico continuam intocados. É também o que
torna a rota completa auditável: cada falha (b), (c) e (d) no Diagrama 2 é a ACL protegendo o
agregado de nascer com um cliente, veículo, serviço ou peça que não existem (ou não estão
disponíveis) no módulo dono daquela informação — sem que OrdensServico precise conhecer as regras
internas de Cadastro ou de PecasInsumos.

**Eventos: domain event dentro do módulo, integration event entre módulos.** `OrcamentoGerado` é
um domain event do agregado `OrdemServico` — só existe dentro de `OrdensServico`, publicado via
`IPublisher` depois do commit (nunca antes, ver [`decisoes.md`](../decisoes.md#transactionbehavior--orquestração-pós-commit)).
Ele tem dois consumidores registrados, ambos rodando na mesma publicação:
`PublicarOrcamentoGerado` (enfileira `OrcamentoGeradoIntegrationEvent` em
`IPendingIntegrationEvents`, publicado logo em seguida pelo `TransactionBehavior` no
`IIntegrationEventBus`) e `EnviarOrcamentoAoCliente` (muta o próprio agregado — chama
`EnviarOrcamento()` e persiste com um `SaveChangesAsync` próprio, fora da UoW original — e dispara
a notificação simulada ao cliente). O `IIntegrationEventBus` é quem cruza a fronteira de módulo: o
handler `DecrementarEstoqueQuandoOrcamentoGerado`, dentro de `PecasInsumos.Application`, reage ao
integration event e chama seu próprio Command (`DecrementarEstoqueCommand`) via `ISender` — nunca
manipula o estoque diretamente a partir de OrdensServico.

**Métrica de negócio.** `OrdensServicoMetrics.RegistrarOrdemAberta(tipo)` é emitida dentro do
Handler, logo após `IOrdemServicoGateway.Adicionar` — antes do commit do `TransactionBehavior`, mas
só no caminho de sucesso. Nenhuma falha de validação ou de ACL chega a incrementar o contador
`oficina.ordens_servico.abertas` (ver [`metricas.md`](../metricas.md)); é assim que o dashboard de
volume diário separa `simples` de `completa` sem contar tentativas rejeitadas.

**Nenhuma divergência entre documentação e código** foi encontrada ao montar este diagrama: o
comportamento acima bate com `clean-architecture.md`, `decisoes.md` e `metricas.md`.
