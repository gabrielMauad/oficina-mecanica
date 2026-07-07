# Refatoração Clean Architecture — Documento de Referência

> **Propósito deste documento.** Esta é a **fonte única de verdade** da refatoração que torna o
> projeto *Oficina Mecânica v2* aderente ao modelo de **Interface Adapters do Robert C. Martin**,
> conforme cobrado pela disciplina. Qualquer sessão futura de trabalho neste tema deve **ler este
> arquivo primeiro** — ele carrega todo o contexto, as decisões, os porquês e as convenções, sem
> necessidade de rediscussão. Os planos de execução (`01-...` a `06-...`) são derivados daqui.

**Data:** 2026-06-24
**Status:** decisões fechadas; implementação não iniciada.

---

## 1. Contexto e objetivo

A 1ª fase entregou um modular monolith em **DDD + CQRS (MediatR) + Ports & Adapters**, com a Regra de
Dependência forçada em compile-time por `ProjectReference`. A 2ª fase exige **Clean Architecture com a
estrutura explícita do Robert Martin** — não basta SOLID e separação de camadas.

O professor apontou que faltam **artefatos nomeados** e que o use case está **implícito**:

> "Os Controllers MVC despacham direto para o MediatR e os Handlers fazem o papel de Use Case de forma
> implícita, **sem Gateways** (a interface entre Use Case e persistência) **nem Presenters** como
> artefatos nomeados."

Fluxo que o professor espera ver (extraído do feedback dele):

```
Endpoint → Controller HTTP → [sem framework daqui pra frente] → Controller (Clean Arch)
        → Use Case → Entity → Gateway → DataSource (Repository)
e a saída voltando via:  Use Case → Presenter → Controller (CA) → Controller HTTP
```

**Objetivo da refatoração:** tornar **Controller CA, Gateway e Presenter** artefatos físicos nomeados,
com o fluxo rastreável a olho nu, **sem abrir mão do MediatR/CQRS** (decisão consciente — ver §3.1).

**Anti-objetivo:** isto é uma refatoração **estrutural**, não de comportamento. Nenhuma regra de
negócio muda. **Os contratos HTTP (shape do JSON de resposta) e os contratos cross-module (`Contracts`)
permanecem idênticos.**

---

## 2. Diagrama de referência (Clean Architecture do Martin) × camadas do projeto

| Anel | Cor | Projeto-alvo por módulo | Pode referenciar framework? |
|---|---|---|---|
| **Entities** | 🟡 | `{Module}.Domain` | Não |
| **Use Cases** | 🔴 | `{Module}.Application` | MediatR/FluentValidation (libs, ok); **sem ASP.NET/EF** |
| **Interface Adapters** | 🟢 | `{Module}.Adapters` *(novo)* + `{Module}.Contracts` | **Não** (sem ASP.NET/EF); MediatR só `ISender` |
| **Frameworks & Drivers** | 🔵 | `{Module}.Infrastructure` + `{Module}.Web` *(ex-Presentation)* + `Bootstrap/Api` | Sim (EF, ASP.NET) |

> Um `.csproj` não representa "um anel" — representa **uma fronteira de dependência que queremos que o
> compilador proteja**. Por isso há mais projetos que anéis (`Contracts`, `SharedKernel`): são eixos
> extras de separação que decidimos enforçar. O anel verde estava **sem assembly próprio** (esfregado
> entre `Infrastructure` e `Presentation`); a refatoração dá identidade a ele com `{Module}.Adapters`.

---

## 3. As 4 decisões (com o porquê)

### 3.1 — MediatR **fica**. O use case continua sendo o Handler.

- **Decisão:** não remover o MediatR. CQRS + pipeline behaviors (`Validation`, `Logging`, `Transaction`)
  são pilar consciente do projeto. O **Handler `IRequestHandler<,>` é o Use Case Interactor**.
- **Como concilia com "sem framework":** o `ISender.Send` é o **único seam de framework** e vive
  **dentro do Controller CA** (anel verde). Os artefatos internos de verdade — Use Case, Entity,
  Gateway, Presenter — permanecem POCOs. A frase honesta é: *"o anel verde é livre de ASP.NET e EF, e
  referencia MediatR (`ISender`) por decisão consciente"*.
- **Risco residual:** a queixa "use case implícito" não some pelo mecanismo, e sim pela **legibilidade**.
  Mitigação: o fluxo `Controller CA → Send → Handler(Use Case) → Gateway → Presenter` fica explícito e
  rastreável; o Handler já é nomeado, um por operação, e delega a decisão ao agregado.
- **Por que é defensável:** o professor reclamou de **artefatos ausentes**, não do MediatR. O
  "sem framework" descreve o fluxo ideal, não um critério de corte.

### 3.2 — Gateways com interface; persistência delega ao Repository (DataSource).

- **Decisão:** todo Gateway tem **interface** (`I{X}Gateway`). O Use Case chama o Gateway; o Gateway
  de persistência **delega** ao `I{Entity}Repository`, que **é o DataSource** (o professor escreve
  literalmente "DataSource (Repository)").
- **O Repository = DataSource já existe** e fica **intocado** (EF, anel azul, em `Infrastructure`). A
  peça que falta é o **Gateway na frente dele**.
- **Onde mora a interface do DataSource (`I{Entity}Repository`): no anel verde (`{Module}.Adapters/DataSources/`),
  não em Application.** Uncle Bob: *uma interface pertence ao seu cliente*. O `I{Entity}Gateway` é
  consumido pelo Use Case → mora em `Application`. Já o `I{Entity}Repository` **só é consumido pelo
  Gateway** (que é verde) — o Use Case nunca o toca. Logo ele pertence ao anel verde, junto do Gateway
  que o usa. Colocá-lo em Application seria declarar, no anel do use case, uma porta que o use case não
  consome (smell de propriedade). A Regra de Dependência é respeitada nos dois lugares (a impl EF no
  Infra aponta para dentro em ambos), mas o verde é o alinhamento correto — e é também onde o projeto de
  referência (`soat-cleanarch`) coloca o `IDataSource`.
- **Por que interface (e não Gateway concreto como no projeto de referência):** no layout multi-projeto,
  um Gateway concreto em `Adapters` consumido pelo Use Case em `Application` **inverteria a dependência**
  (Application→Adapters é proibido). A interface é o que mantém `Adapters → Application` válido em
  compile-time. Não é preferência, é estrutural.
- **Gateway de persistência é fino (delega 1:1) — e tudo bem.** Hoje o EF mapeia o agregado direto (sem
  DTO de persistência), então não há tradução. O Gateway existe por dois motivos: (a) é o **artefato
  nomeado** que o corretor procura; (b) é a **casa futura** do mapeamento se o DTO de persistência for
  introduzido (ver §6, fora de escopo).
- **Gateways de ACL (cross-module) fazem tradução real** — esses "trabalham de verdade". São os atuais
  adapters de `Infrastructure/Acl`, que **mudam de nome para `*Gateway`** e migram para o anel verde.

### 3.3 — Presenter explícito; Handler devolve entidade; ViewModel ≠ DTO de Contracts.

- **Decisão:** o mapeamento de saída (hoje inline no Handler / em construtores `FromX`) vira um
  **Presenter** nomeado no anel verde. O **Handler passa a devolver a entidade/agregado** (ou, para
  leitura projetada, o read model), e o **Presenter monta a ViewModel HTTP**.
- **Regra do retorno do Handler (evita o `*Response` intermediário):**
  - Se o handler **materializa o agregado** (todos os commands **e** as queries que carregam a entidade
    via `I{Entity}Gateway`, ex. `ObterPorId`) → devolve `Result<{Entidade}>`. O Presenter mapeia
    entidade → ViewModel.
  - Se o handler é uma **projeção pura de leitura** (query object dedicado que nunca materializa o
    agregado, ex. `Listar`) → devolve `Result<{ReadModel}>`. O Presenter mapeia read model → ViewModel.
  - **Em nenhum caso** o handler monta a ViewModel nem um `*Response`/DTO intermediário. Se você vê um
    `FromX` sobrevivendo dentro de um handler, é código morto a migrar para o Presenter.
- **Quem chama o Presenter:** o **Controller CA** (espelhando o `CobrancaController` da referência, que
  recebe a entidade do use case e chama `PessoaPresenter.ToResponse`).
- **ViewModel ≠ Contracts DTO:** a ViewModel HTTP é separada do DTO público cross-module. O
  `OrdemServicoResumoDto` (e similares em `Contracts`) **continuam existindo no caminho cross-module**
  (queries consumidas por outros módulos) — só deixam de ser o retorno do Handler HTTP.
- **Compatibilidade:** a ViewModel **preserva os mesmos campos/shape JSON** dos atuais `*Response`, para
  não quebrar os testes de integração nem o contrato HTTP.

### 3.4 — Estrutura: novo projeto `Adapters`; ports → Application; Presentation → Web.

- **Novo `{Module}.Adapters`** (🟢): Controllers CA, Presenters, Gateways (persistência + ACL),
  **DataSources (`I{Entity}Repository`)**, Request/ViewModels. **Sem referência a ASP.NET nem EF**
  (garantia do anel verde provada pelo compilador).
- **Ports migram `Domain` → o anel de quem os consome:** os **Gateways** (`I{X}Gateway`, consumidos pelo
  Use Case) vão para `Application/Gateways/`; a interface de **DataSource** (`I{Entity}Repository`,
  consumida pelo Gateway) vai para `Adapters/DataSources/` (ver §3.2). O `Domain` fica ainda mais puro
  (perde a pasta `Ports` e a interface de repositório).
- **`{Module}.Presentation` → `{Module}.Web`**: é a borda HTTP azul (o "Controller HTTP"). Renomear
  deixa explícito que NÃO é o anel verde.
- **`Domain` e `Application` NÃO são renomeados** — são os nomes idiomáticos do .NET, um corretor mapeia
  `Domain=Entities` e `Application=UseCases` no automático. Renomear seria o maior churn da solução para
  o menor ganho. A legibilidade vai para o nível de **pasta** (ver §5.3).
- **`Contracts` fica intocado** (boundary verde cross-module).
- **DI única por módulo:** o `{Module}Module.cs` em `Infrastructure` **referencia `Adapters`** e registra
  tudo num lugar só (DbContext, `IRepository`→EF, MediatR, validators, **e** `IGateway`→Gateway,
  Controller CA, Presenter). `Infrastructure → Adapters` é outer→inner, **não há ciclo** (Adapters nunca
  referencia Infrastructure).

---

## 4. Mapa de projetos por módulo (estado-alvo)

| Projeto | Anel | Conteúdo | Referencia | Framework |
|---|---|---|---|---|
| `.Domain` | 🟡 | agregados, VOs, eventos de domínio | `SharedKernel.Domain` | nenhum |
| `.Application` | 🔴 | Handlers, Commands/Queries, Validators, **`Gateways/` (interfaces `I{X}Gateway`)**, read models de query, Errors, EventHandlers | `SharedKernel.Application`, `.Domain`, `.Contracts` | MediatR, FluentValidation |
| **`.Adapters`** *(novo)* | 🟢 | `Controllers/` (CA), `Gateways/` (impls), `Presenters/`, **`DataSources/` (`I{Entity}Repository`)**, `Models/` (Request + ViewModel) | `.Application`, `.Domain`, Contracts de outros módulos, MediatR | **sem ASP.NET/EF** |
| `.Infrastructure` | 🔵 | DbContext, EF config, `Repositories/` (impl = DataSource), `Queries/` (impl Contracts), `{Module}Module.cs` (DI) | `SharedKernel.Application`, `.Application`, `.Domain`, `.Contracts`, **`.Adapters`**, EF/Npgsql | EF, Npgsql |
| `.Web` *(ex-`.Presentation`)* | 🔵 | MVC "Controller HTTP", mapeamento `Result`→status, AssemblyMarker | `.Adapters`, `SharedKernel.Domain` (Result) | ASP.NET |
| `.Contracts` | 🟢 | DTOs cross-module, IntegrationEvents, Query interfaces | `SharedKernel.Domain` | nenhum |

### Grafo de dependência (sem ciclos)

```
Web(🔵) ─► Adapters(🟢) ─► Application(🔴) ─► Domain(🟡) ─► SharedKernel.Domain
Infrastructure(🔵) ─► Adapters(🟢) ─► Application(🔴) ─► Domain(🟡)
Infrastructure(🔵) ─► Application/Domain/Contracts (implementa I{Entity}Repository, IxQuery)
Bootstrap/Api (composition root) ─► todos
Adapters NUNCA referencia Infrastructure nem Web.
```

---

## 5. Convenções (regras que TODOS os planos seguem)

### 5.1 Fluxo de uma operação (exemplo: `RegistrarDiagnostico`)

```
[Web 🔵] {Entity}ApiController (MVC)
   → recebe HTTP, monta o Request Model (record framework-free)
   → chama o Controller CA
   → traduz Result<ViewModel> em status HTTP (decisão POR ENDPOINT — ver 5.2)

[Adapters 🟢] {Entity}Controller (CA, POCO)
   → monta o Command a partir do Request Model
   → result = await _sender.Send(command)        // único seam MediatR
   → if result.IsFailure return result.Error
   → return {Entity}Presenter.Present(result.Value)   // entidade → ViewModel

[Application 🔴] {Operacao}Handler (Use Case)
   → carrega agregado via I{Entity}Gateway (persistência)
   → consulta externos via I{X}Gateway (ACL)
   → agregado.{RegraDeNegocio}(...)               // decisão no Domain
   → await gateway.Atualizar(agregado)
   → return Result<{Agregado}>                     // devolve ENTIDADE (não DTO)

[Adapters 🟢] {Entity}Gateway : I{Entity}Gateway
   → delega para I{Entity}Repository (DataSource)

[Infrastructure 🔵] {Entity}Repository : I{Entity}Repository
   → EF (intocado)
```

### 5.2 Mapeamento de erro → status HTTP **permanece no Web (MVC)**

O `Error` (SharedKernel) **não tem categoria** — `Validation`/`NotFound`/`Conflict` têm o mesmo shape.
Portanto o status é escolhido **por endpoint** no MVC, exatamente como hoje (`ObterPorId`→`NotFound`,
`Criar`→`UnprocessableEntity`, `Desativar`→`NoContent`/`NotFound`). **Não centralizar status a partir do
`Error`.** O Controller CA devolve só `Result<ViewModel>`; quem decide o código HTTP é a borda azul.

### 5.3 Nomenclatura

| Artefato | Nome | Local |
|---|---|---|
| Controller CA | `{Entity}Controller` (POCO, sem atributos MVC) | `{Module}.Adapters/Controllers/` |
| Controller HTTP (MVC) | `{Entity}ApiController` (`[ApiController]`, `[Route]`) | `{Module}.Web/Controllers/` |
| Presenter | `{Entity}Presenter` (estático ou injetável), método `Present(...)` | `{Module}.Adapters/Presenters/` |
| Gateway (interface) | `I{X}Gateway` | `{Module}.Application/Gateways/` |
| Gateway (impl persistência) | `{Entity}Gateway` (delega ao repo) | `{Module}.Adapters/Gateways/` |
| Gateway (impl ACL) | `{X}Gateway` (ex-`{X}Adapter`) | `{Module}.Adapters/Gateways/` |
| DataSource (interface) | `I{Entity}Repository` (movido do Domain) | `{Module}.Adapters/DataSources/` |
| DataSource (impl) | `{Entity}Repository` (EF, intocado) | `{Module}.Infrastructure/Persistence/Repositories/` |
| Request Model | `{Operacao}Request` (record sem atributos) | `{Module}.Adapters/Models/` |
| ViewModel | `{Operacao}ViewModel` (mesmo shape do antigo `*Response`) | `{Module}.Adapters/Models/` |

> Pastas internas de `Adapters` espelham as caixas verdes do Martin: `Controllers/`, `Gateways/`,
> `Presenters/`. É a legibilidade que substitui o rename de `Application→UseCases`.

### 5.4 Regras invariantes (valem ao FIM de cada plano)

1. **Build da solução verde.**
2. **Todos os testes verdes** (unit + integração).
3. **Contrato HTTP preservado** (shape JSON idêntico ao atual).
4. **Contratos cross-module (`Contracts`) inalterados.**
5. `Domain` sem dependência de framework; `Adapters` sem ASP.NET/EF.
6. Regra de Dependência intacta (provada pelos `ProjectReference`).
7. Nenhuma regra de negócio alterada.

---

## 6. Fora de escopo (explícito)

- ~~**DTO de persistência** (separar entidade do modelo EF)~~ — **feito** em `07-plano-dtos-persistencia.md`
  (2026-07-07): Records em `{Module}.Adapters/DataSources/Records/`, `Reconstituir` no domínio,
  `IDomainEventCollector` desacoplando eventos do `ChangeTracker`, reconciliação de agregado com filhos no
  `OrdemServicoRepository`. Ver esse documento para detalhes.
- **Renomear `Domain`/`Application`** (para `Entities`/`UseCases`): descartado (§3.4).
- **Alterar `SharedKernel`**: intocado (Result, behaviors, Entity/AggregateRoot permanecem).
- **Mudar comportamento, regras de negócio, rotas ou shape de resposta.**
- **Mexer em migrations/EF mappings** (a persistência fica como está).

---

## 7. Índice e ordem dos planos de execução

Cada plano termina num estado **compilável e testado** (checkpoint válido). Módulos são independentes
(modular monolith) e os `Contracts` são estáveis, então **OrdensServico não depende de Cadastro/PecasInsumos
estarem prontos** — a ordem abaixo é por didática (piloto primeiro) e tamanho de sessão.

| # | Plano | Escopo | Pré-condição |
|---|---|---|---|
| 01 | **Fundação + PecasInsumos (piloto)** | Aplica TODAS as convenções end-to-end no módulo médio e autocontido. Serve de template de referência. | — |
| 02 | **Cadastro — estrutural + fluxo Cliente** | Cria `Adapters`/`Web`/DI do módulo + gateways de persistência (3) + fluxo completo do agregado Cliente. | 01 |
| 03 | **Cadastro — fluxo Servico + Veiculo** | Fluxo completo dos agregados Servico e Veiculo (reutiliza estrutura do 02). | 02 |
| 04 | **OrdensServico — estrutural** | `Adapters`/`Web`/DI + 6 Gateways ACL (renomeados) + Gateway de persistência + ports→Application + handlers religados aos gateways (ainda devolvendo DTO). | 01 |
| 05a | **OrdensServico — fluxo (commands)** | Controller CA + Presenter + ViewModels + handlers dos **8 commands** devolvendo entidade + slim dos 8 endpoints MVC. Queries seguem no fluxo antigo (híbrido). | 04 |
| 05b | **OrdensServico — fluxo (queries) + fechamento do módulo** | 2 queries no Controller CA + presentation fina + slim dos 2 endpoints MVC; remove `Send` direto restante; código morto. | 05a |
| 06 | **Autenticacao + finalização global** | Módulo Autenticacao (CA controller + presenter + Web rename, sem gateway) + auditoria final: build solução, todos os testes, auditoria da Regra de Dependência, limpeza de código morto, atualização de docs. | 02,03,04,05a,05b |

> **Sem furos:** cada plano lista exaustivamente arquivos a criar/mover/modificar/deletar e declara o que
> é "fora de escopo / tratado em outro plano". A soma dos 6 planos cobre os 4 módulos + host + testes +
> docs. Nada fora deles.

---

## 8. Checklist mestre de cobertura (garantia de completude)

Por módulo, a refatoração precisa contemplar:

- [ ] Ports `Domain.Ports` → `Application/Gateways/` (renomeados `I{X}Gateway`).
- [ ] `I{Entity}Repository` `Domain` → `Adapters/DataSources/` (cliente é o Gateway, não o use case — ver §3.2).
- [ ] Projeto `{Module}.Adapters` criado e referenciado por `Infrastructure` + `Web`.
- [ ] Gateways de persistência (interface em App, impl em Adapters delegando ao repo).
- [ ] Gateways de ACL renomeados `*Gateway` e movidos `Infrastructure/Acl` → `Adapters/Gateways`.
- [ ] Handlers: dependências trocadas para `I{X}Gateway`; retorno `Result<DTO>` → `Result<Entidade>`.
- [ ] Presenters criados (absorvem o `FromX`/montagem inline).
- [ ] Controllers CA criados (Send + Presenter).
- [ ] Request/ViewModels no anel verde (ViewModel = shape do antigo `*Response`).
- [ ] `{Module}.Presentation` renomeado `{Module}.Web`; MVC controllers `*ApiController` finos.
- [ ] DI do `{Module}Module.cs` registra gateways, controller CA e presenters.
- [ ] `Program.cs`: `AddApplicationPart` aponta para o assembly `.Web` renomeado.
- [ ] Testes do módulo atualizados (usings de ports movidos, retorno dos handlers).

Itens globais (Plano 06):

- [ ] Solução inteira compila; todos os testes (unit + `tests/IntegrationTests`) verdes.
- [ ] `Adapters` de nenhum módulo referencia ASP.NET/EF (auditoria).
- [ ] `Domain` de nenhum módulo referencia framework.
- [ ] Código morto removido (antigos `*Response` com `FromX` que viraram Presenter, `Models` órfãos).
- [ ] `docs/arquitetura/analise-clean-architecture.md` e README atualizados ao novo desenho.
