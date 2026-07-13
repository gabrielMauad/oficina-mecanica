# Plano 04 — OrdensServico: estrutural (gateways, ACL, ports, projeto)

> Ler `00-referencia.md`. Este é o plano estrutural mais pesado por causa dos **6 gateways de ACL**.
> NÃO toca no fluxo Controller CA/Presenter/retorno-de-handler — isso é o Plano 05. Aqui montamos o anel
> verde (projeto + gateways), movemos os ports e **religamos os handlers aos gateways mantendo o retorno
> atual (`Result<DTO>`)**. Estado final compila e tem comportamento idêntico.

**Pré-condição:** Plano 01 concluído (template). Independe de Cadastro (Contracts são estáveis).
**Estado final:** `OrdensServico.Adapters` criado com os gateways; ports em Application; handlers usando
gateways; MVC ainda chamando `Send` direto (fluxo antigo); build + testes verdes.

---

## Escopo (estrutural)

Agregado `OrdemServico`. **6 dependências externas (ACL)** hoje em `Infrastructure/Acl`:
`ClienteInfoAdapter`, `VeiculoInfoAdapter`, `ServicoInfoAdapter`, `PecaDisponibilidadeAdapter`,
`PecaInsumoInfoAdapter`, `NotificacaoClienteAdapter`. Ports: 5 em `Domain/Ports/`
(`IClienteInfoPort`, `IVeiculoInfoPort`, `IServicoInfoPort`, `IPecaDisponibilidadePort`,
`IPecaInsumoInfoPort`) + **`INotificacaoClientePort` já em `Application/Ports/`**. Persistência:
`IOrdemServicoRepository` (Domain).

### Fora de escopo
- Controller CA, Presenters, mudança de retorno dos handlers, Request/ViewModels, slim do MVC → **Plano 05**.
- `DomainEventHandlers/` (4) — inalterados.
- `Contracts` (DTOs, IntegrationEvents, queries) — inalterado.

---

## Arquivos

### Criar
- `OrdensServico.Adapters/OrdensServico.Adapters.csproj` (refs: `OrdensServico.Application`, `OrdensServico.Domain`, `Cadastro.Contracts`, `PecasInsumos.Contracts`, MediatR; **sem** ASP.NET/EF).
- `OrdensServico.Application/Gateways/` — interfaces renomeadas dos ports:
  `IClienteGateway`, `IVeiculoGateway`, `IServicoGateway`, `IPecaDisponibilidadeGateway`,
  `IPecaInsumoInfoGateway`, `INotificacaoClienteGateway`, **`IOrdemServicoGateway`** (persistência).
- `OrdensServico.Adapters/Gateways/` — impls:
  - `ClienteGateway` (ex-`ClienteInfoAdapter`), `VeiculoGateway`, `ServicoGateway`,
    `PecaDisponibilidadeGateway`, `PecaInsumoInfoGateway`, `NotificacaoClienteGateway` — **lógica de
    tradução idêntica à atual**, só renomeadas e movidas.
  - `OrdemServicoGateway` (`: IOrdemServicoGateway`) — delega a `IOrdemServicoRepository`.

### Mover / renomear
- `OrdensServico.Domain/Ports/I*Port.cs` (5) → `OrdensServico.Application/Gateways/I*Gateway.cs` (renomear interface; manter DTOs auxiliares `Ports/Dtos/*` — ver nota).
- `OrdensServico.Domain/OrdemServico/IOrdemServicoRepository.cs` → `OrdensServico.Adapters/DataSources/IOrdemServicoRepository.cs` (anel verde — cliente é o `OrdemServicoGateway`; ver `00-referencia §3.2`).
- `OrdensServico.Application/Ports/INotificacaoClientePort.cs` → `Application/Gateways/INotificacaoClienteGateway.cs`.
- `OrdensServico.Infrastructure/Acl/*Adapter.cs` → removidos (lógica migrada para `Adapters/Gateways/*Gateway.cs`).
- `OrdensServico.Presentation/` → **`OrdensServico.Web`** (csproj, pasta, `OrdensServicoAssemblyMarker`).
  *(o MVC controller continua no formato antigo neste plano — só muda de projeto.)*

> **Nota sobre os DTOs dos ports** (`Domain/Ports/Dtos/ClienteInfo.cs`, `PecaDisponibilidade.cs`,
> `PecaInsumoInfo.cs` e `Application/Ports/Dtos/*`): são tipos de entrada/saída dos gateways. Movê-los
> junto para `Application/Gateways/Dtos/`. Verificar todos os usings (handlers consomem esses DTOs).

### Modificar
- **Handlers (10):** trocar injeção dos `I*Port`/`IOrdemServicoRepository` pelos `I*Gateway`/`IOrdemServicoGateway`. **Retorno permanece `Result<OrdemServicoResumoDto>` (inalterado neste plano).** Apenas tipos de dependência e usings mudam.
- `OrdensServico.Infrastructure/Persistence/Repositories/OrdemServicoRepository.cs` — atualizar `using` da interface (agora em `OrdensServico.Adapters.DataSources`; o Infra já referencia `Adapters`).
- `OrdensServico.Infrastructure/OrdemServicoModule.cs` — referenciar `OrdensServico.Adapters`; registrar os 6 `I*Gateway`→impls + `IOrdemServicoGateway`→`OrdemServicoGateway`. Remover registros antigos dos `*Adapter` de ACL.
- `src/Bootstrap/Api/Program.cs` — `AddApplicationPart(typeof(OrdensServicoAssemblyMarker).Assembly)` → assembly `OrdensServico.Web`.
- Solução: adicionar `OrdensServico.Adapters`, renomear `Presentation`→`Web`.

---

## Passos ordenados
1. Criar `OrdensServico.Adapters.csproj`; adicionar à solução.
2. Mover os 5 ports + `INotificacaoClientePort` + DTOs de ports para `Application/Gateways/` (renomear interfaces para `I*Gateway`); mover `IOrdemServicoRepository` para `Adapters/DataSources/`.
3. Migrar a lógica dos 6 `*Adapter` (ACL) para `Adapters/Gateways/*Gateway.cs` (renome + move, lógica idêntica). Criar `OrdemServicoGateway` (delega ao repo).
4. Religar os 10 handlers aos gateways (só dependências/usings). **Não** mexer no retorno.
5. Atualizar repo (using) e `OrdemServicoModule.cs` (refs + registros; remover registros dos `*Adapter`).
6. Renomear `Presentation`→`Web`; ajustar `Program.cs`.
7. **Build solução + testes.** Comportamento idêntico esperado.

## Testes
- `OrdemServico.Application.Tests` — **grande impacto de usings**: mocks de `I*Port` viram `I*Gateway`; `IOrdemServicoRepository` agora em Application. Ajustar todos os usings/nomes de mock. Asserts inalterados (retorno dos handlers não mudou).
- `OrdemServico.Domain.Tests` — inalterado.
- Integração — comportamento idêntico, verde.

## Definition of Done
- [ ] `OrdensServico.Adapters` criado; `Web` renomeado; build verde.
- [ ] 6 gateways de ACL renomeados/movidos com lógica idêntica; `OrdemServicoGateway` de persistência criado.
- [ ] Ports (Gateways) + DTOs em Application; `IOrdemServicoRepository` (DataSource) em Adapters; `Domain/Ports/` eliminada.
- [ ] Handlers usando gateways; **retorno ainda `Result<...Dto>`** (fluxo será trocado no 05).
- [ ] Registros de ACL antigos removidos do DI; testes verdes; comportamento idêntico.
