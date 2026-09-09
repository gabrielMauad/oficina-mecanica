# Métricas de negócio

> Contrato de instrumentação criado na Fase 3, item "Dashboards" (volume diário de OS, tempo médio
> de execução por etapa, erros e falhas nas integrações) e "alertas para falhas no processamento de
> ordens de serviço". Ver `docs/planos/fase-3/00-analise-da-spec.md`, seções 1.4 e 3.2-B.

A instrumentação usa apenas a API nativa do .NET, `System.Diagnostics.Metrics` (`Meter`,
`Counter<T>`, `Histogram<T>`) — **não depende do OpenTelemetry**. O OTel (ou o agente do
vendor de observabilidade escolhido) é responsável só por *coletar e exportar* essas métricas;
essa tarefa fica para a instrumentação de OTel feita em paralelo, que usa os nomes de `Meter`
abaixo para descobrir os instrumentos publicados pela aplicação.

## Meters

| Meter | Onde vive | Classe |
|---|---|---|
| `OficinaMecanica.OrdensServico` | `OrdensServico.Application` | `OrdensServico.Application.Metrics.OrdensServicoMetrics` |
| `OficinaMecanica.Integracoes` | `SharedKernel.Application` | `SharedKernel.Application.Metrics.IntegracoesMetrics` |

Cada classe encapsula um único `Meter` singleton (registrado via `AddOrdensServicoModule` /
`AddSharedKernelServices`, seguindo o mesmo padrão de registro dos demais serviços do módulo) e o
descarta corretamente em `Dispose()` — o container de DI chama `Dispose()` automaticamente ao
encerrar a aplicação, por ser singleton.

## Instrumentos

### `oficina.ordens_servico.abertas`

- **Tipo:** `Counter<long>` · **Unidade:** `{ordem}`
- **Descrição:** "Ordens de serviço abertas"
- **Tag:** `tipo` = `simples` | `completa`
- **Alimenta:** volume diário de ordens de serviço no dashboard (soma por dia, quebrada por tipo
  de abertura).
- **Emitido em:**
  - `GerarOrdemServicoHandler` com `tipo=simples`, após `IOrdemServicoGateway.Adicionar` ter
    sucesso.
  - `AbrirOrdemServicoCompletaHandler` com `tipo=completa`, após `IOrdemServicoGateway.Adicionar`
    ter sucesso.
- Nada é emitido se o `Result` do handler for falha (cliente/veículo inválido, serviço ou peça
  indisponível, etc.) — o incremento só acontece depois que a OS já foi persistida.

### `oficina.ordens_servico.etapa.duracao`

- **Tipo:** `Histogram<double>` · **Unidade:** `s` (segundos)
- **Descrição:** "Duração de cada etapa da ordem de serviço"
- **Tag:** `etapa` = `diagnostico` | `execucao` | `finalizacao`
- **Alimenta:** tempo médio de execução por etapa (Diagnóstico / Execução / Finalização) no
  dashboard.
- **Emitido em:**

  | Etapa | Intervalo medido | Handler |
  |---|---|---|
  | `diagnostico` | de `EmDiagnostico` até o diagnóstico ser registrado | `RegistrarDiagnosticoHandler` |
  | `execucao` | de `EmExecucao` até `Finalizada` | `FinalizarOrdemServicoHandler` |
  | `finalizacao` | de `Finalizada` até `Entregue` | `ConcluirOrdemServicoHandler` |

  Em todos os três, nada é emitido se a transição de domínio (`RegistrarDiagnostico`,
  `Finalizar`, `Concluir`) retornar falha.

#### Como a duração é derivada — e a limitação que isso traz

A entidade `OrdemServico` (`OrdensServico.Domain`) não guarda um timestamp por etapa: só existe
`CriadoEm` e `AtualizadoEm`, e `AtualizadoEm` é reescrito com `DateTime.UtcNow` **a cada** método de
transição de estado (`IniciarDiagnostico`, `RegistrarDiagnostico`, `EnviarOrcamento`,
`AprovarOrcamento`, `RejeitarOrcamento`, `Executar`, `Finalizar`, `NotificarCliente`, `Concluir`).

Não alteramos o domínio para adicionar timestamps por etapa — mudar o modelo relacional nesta fase
exigiria uma decisão arquitetural documentada à parte. Em vez disso, cada handler lê
`ordemServico.AtualizadoEm` **antes** de chamar o método de domínio que faz a transição (depois da
chamada o valor já foi sobrescrito) e calcula `DateTime.UtcNow - inicioEtapa` como proxy da duração
da etapa.

Essa aproximação é exata quando nenhuma outra operação de domínio toca `AtualizadoEm` entre o início
e o fim da etapa. Isso vale integralmente para `execucao` (nada mais atualiza a OS enquanto ela está
`EmExecucao`), mas tem duas limitações conhecidas:

- **`diagnostico`**: no caminho feliz (`IniciarDiagnostico` → `RegistrarDiagnostico`), a medida
  reflete corretamente o tempo em diagnóstico. Mas `RegistrarDiagnostico` também aceita OS no status
  `AguardandoAprovacao` (recadastro de diagnóstico após um orçamento ser rejeitado — ver
  `RejeitarOrcamento`). Nesse caminho alternativo, `AtualizadoEm` já foi sobrescrito pela rejeição do
  orçamento, então a métrica passa a medir "tempo desde a rejeição do orçamento até o novo
  diagnóstico", não o tempo total em diagnóstico.
- **`finalizacao`**: `Concluir` exige que a OS já tenha sido notificada (`NotificarCliente`), e
  `NotificarCliente` também sobrescreve `AtualizadoEm`. Como `NotificarCliente` é sempre chamado
  depois de `Finalizar` e antes de `Concluir`, a métrica **não mede o intervalo real entre
  finalização e entrega** — mede o intervalo entre a notificação ao cliente e a entrega/conclusão.
  Para medir o intervalo real seria necessário um timestamp dedicado (`FinalizadoEm`), o que fica
  fora do escopo desta tarefa.

Essas duas limitações devem ser lidas como "o dashboard de tempo médio de finalização e de
recadastro de diagnóstico após rejeição de orçamento tem viés conhecido", não como um bug — o
contrato de métricas prioriza não alterar o schema de domínio nesta fase.

### `oficina.integracoes.falhas`

- **Tipo:** `Counter<long>` · **Unidade:** `{falha}`
- **Descrição:** "Falhas no processamento de eventos de integração"
- **Tags:** `evento` (nome do tipo do `IIntegrationEvent`, ex.: `OrdemServicoFinalizada`) e
  `handler` (nome do tipo do `IIntegrationEventHandler` que falhou)
- **Alimenta:** "erros e falhas nas integrações" no dashboard, e serve de base para o alerta de
  "falha no processamento de ordens de serviço" (alertar quando a taxa desse contador subir).
- **Emitido em:** `InMemoryIntegrationEventBus.Publish<T>`, que agora envolve a chamada a cada
  handler em `try/catch`: se o handler lançar, o contador é incrementado e **a exceção é
  relançada** — o bus não engole o erro nem muda a semântica transacional existente (o
  `TransactionBehavior` continua vendo a falha e revertendo a `UnitOfWork` normalmente). Handlers
  seguintes na mesma publicação não são executados, exatamente como antes desta mudança.

## Convenções seguidas

- Nomes de instrumento em minúsculas, separados por ponto; unidades em UCUM (`{ordem}`, `s`,
  `{falha}`).
- Nenhuma tag carrega dado pessoal ou de alta cardinalidade: `tipo` e `etapa` são enums fechados
  de poucos valores; `evento` e `handler` são nomes de tipo (finito, conhecido em tempo de
  compilação) — nunca CPF, nome, e-mail ou id de cliente/ordem de serviço.

## Testes

Cada instrumento tem cobertura em testes unitários usando `MetricCollector<T>`
(`Microsoft.Extensions.Diagnostics.Metrics.Testing`), que assina o `Meter` público exposto por
`OrdensServicoMetrics.Meter` / `IntegracoesMetrics.Meter` e verifica valor + tags:

- `tests/Modules/OrdensServico/OrdemServico.Application.Tests/Commands/GerarOrdemServicoHandlerTests.cs`
- `tests/Modules/OrdensServico/OrdemServico.Application.Tests/Commands/AbrirOrdemServicoCompletaHandlerTests.cs`
- `tests/Modules/OrdensServico/OrdemServico.Application.Tests/Commands/RegistrarDiagnosticoHandlerTests.cs`
- `tests/Modules/OrdensServico/OrdemServico.Application.Tests/Commands/FinalizarOrdemServicoHandlerTests.cs`
- `tests/Modules/OrdensServico/OrdemServico.Application.Tests/Commands/ConcluirOrdemServicoHandlerTests.cs`
- `tests/SharedKernel/SharedKernel.Application.Tests/InMemoryIntegrationEventBusTests.cs`

Os testes cobrem tanto o caminho de sucesso (valor e tags corretos) quanto o de falha (nenhuma
métrica emitida quando o handler retorna erro / lança exceção), além de confirmar que a exceção
continua sendo propagada pelo `InMemoryIntegrationEventBus`.
