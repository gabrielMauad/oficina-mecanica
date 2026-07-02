# Análise de Aderência à Clean Architecture

> Análise técnica do projeto **Sistema de Oficina Mecânica** frente aos princípios da
> Clean Architecture (Robert C. Martin). O objetivo é confrontar cada anel/camada do
> diagrama clássico com as camadas reais do projeto, avaliando responsabilidades e,
> sobretudo, a **Regra de Dependência** (dependências sempre apontam para dentro).

**Data:** 2026-06-15

---

## 1. Veredito

**O projeto é aderente à Clean Architecture.** Os quatro anéis estão representados por
projetos físicos distintos, e a Regra de Dependência é respeitada e **forçada em
compile-time** pelas referências de projeto (`ProjectReference`) — não é apenas uma
convenção documentada, é uma fronteira que o compilador impede de violar.

O projeto vai além do mínimo: aplica **Ports & Adapters (Arquitetura Hexagonal)** de
forma textbook no fluxo de controle interno (MediatR como mecanismo de inversão entre
Controller → Use Case) e na comunicação entre módulos (ACL via ports no Domain +
adapters na Infrastructure).

A única ressalva relevante (detalhada na §6) é nomenclatura/organização, não violação
de princípio: as *ports* de ACL residem em `Domain.Ports` em vez de `Application`.
Isso é uma escolha hexagonal legítima, não uma quebra da Regra de Dependência.

---

## 2. Mapeamento dos anéis da Clean Architecture → camadas do projeto

| Anel da Clean Architecture | Cor no diagrama | Camada(s) no projeto | Projetos `.csproj` |
|---|---|---|---|
| **Enterprise Business Rules** (Entities) | 🟡 Amarelo | **Domain** | `*.Domain`, `SharedKernel.Domain` |
| **Application Business Rules** (Use Cases) | 🔴 Vermelho | **Application** | `*.Application`, `SharedKernel.Application` |
| **Interface Adapters** (Controllers, Gateways, Presenters) | 🟢 Verde | **Presentation** + **Infrastructure** (adapters/repos/contracts) | `*.Presentation`, `*.Infrastructure`, `*.Contracts` |
| **Frameworks & Drivers** (DB, Web, Devices) | 🔵 Azul | **Bootstrap** + EF Core/ASP.NET/Postgres | `Bootstrap/Api`, pacotes NuGet |

---

## 3. A Regra de Dependência (verificação por `ProjectReference`)

A prova mais forte de aderência: as dependências, conferidas nos `.csproj`, apontam
**sempre para dentro**.

```
Bootstrap/Api  ──►  Infrastructure  ──►  Application  ──►  Domain  ──►  SharedKernel.Domain
   (azul)              (verde)            (vermelho)       (amarelo)        (núcleo)
                          │                   │
                          └──► Contracts ◄────┘   (interface pública, depende só de SharedKernel.Domain)
```

Evidências (módulo OrdensServico, idêntico nos demais):

| Projeto | Referencia | Comentário |
|---|---|---|
| `OrdensServico.Domain` | **apenas** `SharedKernel.Domain` | ✅ Núcleo puro. Sem EF, sem MediatR, sem ASP.NET. |
| `OrdensServico.Application` | `Domain`, `Contracts`, `SharedKernel.Application` | ✅ Olha só para dentro (Domain) e para abstrações. |
| `OrdensServico.Contracts` | **apenas** `SharedKernel.Domain` | ✅ Contrato público sem acoplamento de implementação. |
| `OrdensServico.Infrastructure` | `Application`, `Domain` (transitivo), `Contracts`, EF/Npgsql | ✅ Anel externo conhece os internos, nunca o contrário. |
| `OrdensServico.Presentation` | **apenas** `Application` | ✅ Controller não enxerga Domain nem Infrastructure. |

E o ponto decisivo: **`SharedKernel.Domain` referencia zero projetos** (só
`MediatR.Contracts`, que é apenas a interface marcadora `INotification` para domain
events) — é o centro absoluto, exatamente o que o anel amarelo exige.

> Nenhum módulo referencia o `Domain` ou `Application` de outro módulo. A comunicação
> cross-module passa exclusivamente por `Contracts` (interface pública), o que mantém a
> Regra de Dependência válida inclusive *entre* bounded contexts.

---

## 4. Análise camada por camada

### 4.1 🟡 Enterprise Business Rules — `Domain`

**Correspondência: Entities (núcleo amarelo).**

`OrdemServico.cs` é um **Aggregate Root** clássico: estado encapsulado (setters
privados), coleções expostas como `IReadOnlyList`, construção via factory
(`Criar`) e **invariantes protegidas por métodos de negócio** (`IniciarDiagnostico`,
`RegistrarDiagnostico`, `AprovarOrcamento`, `Executar`, `Finalizar`…). Toda transição
de estado valida pré-condições e retorna `Result<T>` — regras de negócio
**empresariais**, independentes de qualquer caso de uso ou tecnologia.

Aderências verificadas:
- ✅ **Zero dependências de framework.** Nenhum `using` de EF, ASP.NET, MediatR concreto
  ou HTTP. Apenas `SharedKernel.Domain`.
- ✅ **Regras de negócio puras.** A máquina de estados da OS e o cálculo de valor do
  orçamento vivem no agregado, não em serviços.
- ✅ **Domain events** (`DiagnosticoConcluido`, `OrcamentoRejeitado`,
  `OrdemServicoFinalizada`) nascem dentro do agregado via `AddDomainEvent`.
- ✅ **Interfaces de repositório** (`IOrdemServicoRepository`) definidas no Domain —
  o Domain declara *o que precisa*, a Infrastructure implementa. Inversão de dependência
  perfeita.

Este é o anel mais bem executado do projeto.

### 4.2 🔴 Application Business Rules — `Application`

**Correspondência: Use Cases (Interactors).**

Cada caso de uso é um *vertical slice* `Command/Query + Handler + Validator` (CQRS via
MediatR). O `RegistrarDiagnosticoHandler` exemplifica o papel do **Use Case Interactor**
do diagrama:

1. orquestra (busca o agregado no repositório, consulta ports de outros módulos);
2. delega a **decisão de negócio** ao agregado (`ordemServico.RegistrarDiagnostico(...)`);
3. persiste e devolve um **DTO** (`OrdemServicoResumoDto`) — nunca o agregado cru.

Aderências verificadas:
- ✅ **Depende de abstrações, não de implementações:** `IServicoInfoPort`,
  `IPecaDisponibilidadePort`, `IOrdemServicoRepository` — todas interfaces, injetadas.
- ✅ **Não contém regra de negócio empresarial** — apenas orquestração e regras *de
  aplicação* (qual ordem chamar, o que fazer com falha de disponibilidade).
- ✅ **Não conhece HTTP nem EF.** O handler não sabe que existe um controller ou um
  Postgres do outro lado.
- ✅ **Pipeline behaviors** (`Validation`, `Logging`, `Transaction`) no
  `SharedKernel.Application` são *cross-cutting concerns* do anel de aplicação,
  corretamente posicionados.

### 4.3 🟢 Interface Adapters — `Presentation`, `Infrastructure`, `Contracts`

**Correspondência: Controllers, Presenters e Gateways (anel verde).**

Este anel converte dados entre o formato dos casos de uso e o formato do mundo externo
(web e banco).

**Controllers (`Presentation`)** — `OrdensServicoController` é um Controller de livro:
fino, sem regra de negócio. Recebe o HTTP, monta o Command, despacha via `ISender`
(MediatR) e traduz `Result` em status HTTP (`Ok`, `UnprocessableEntity`, `NotFound`,
`CreatedAtAction`). Referencia **apenas** `Application`.

**Gateways (`Infrastructure`)** — duas formas de adapter, ambas implementando interfaces
definidas mais para dentro:
- `OrdemServicoRepository : IOrdemServicoRepository` — gateway de persistência (EF Core).
- `ClienteInfoAdapter : IClienteInfoPort` — **Anti-Corruption Layer**: traduz o contrato
  do módulo Cadastro (`ICadastroClienteQuery`) para o vocabulário do módulo OrdensServico
  (`ClienteInfo`). É o "Gateway" do diagrama em estado puro.

**Contracts** — interface pública do módulo (queries síncronas + integration events).
Funciona como a fronteira de adaptação *entre* bounded contexts.

Aderências verificadas:
- ✅ Toda classe aqui **implementa uma abstração** de um anel interno.
- ✅ Conversão de dados (entidade ↔ DTO, contrato externo ↔ modelo interno) acontece
  exatamente neste anel, como manda o diagrama.
- ✅ A dependência aponta para dentro: Infrastructure→Application→Domain.

### 4.4 🔵 Frameworks & Drivers — `Bootstrap/Api`

**Correspondência: Web, DB, External Interfaces (anel azul externo).**

`Program.cs` é a **Composition Root**: configura JWT, OpenAPI, health checks, registra
cada módulo (`AddOrdensServicoModule`, etc.), pluga os controllers via
`AddApplicationPart` e aplica migrations. É o único lugar onde tudo se conecta — e o mais
externo, descartável e específico de tecnologia.

A injeção de dependência (`OrdemServicoModule.cs`) é onde as **interfaces dos anéis
internos são ligadas às implementações concretas** (`IClienteInfoPort` →
`ClienteInfoAdapter`, `IUnitOfWork` → `OrdensServicoDbContext`). Esse é o mecanismo
prático que faz a inversão de dependência funcionar em runtime.

Aderências verificadas:
- ✅ Frameworks (ASP.NET, EF, Npgsql, MediatR) ficam confinados ao anel externo e à
  Infrastructure. Domain e Application permanecem agnósticos.
- ✅ O detalhe "PostgreSQL" é substituível sem tocar nos anéis internos.

---

## 5. Fluxo de controle vs. diagrama (Input Port / Output Port / Presenter)

O canto inferior direito do diagrama mostra o fluxo:
`Controller → Use Case Input Port → Interactor → Use Case Output Port → Presenter`.

O projeto implementa esse fluxo com **MediatR** como mecanismo de inversão:

| Elemento do diagrama | Implementação no projeto |
|---|---|
| **Controller** | `OrdensServicoController` |
| **Use Case Input Port** | A interface `IRequest<Result<T>>` do Command (resolvida por `ISender.Send`) |
| **Use Case Interactor** | `RegistrarDiagnosticoHandler` (`IRequestHandler<,>`) |
| **Use Case Output Port** | O tipo de retorno `Result<OrdemServicoResumoDto>` |
| **Presenter** | O Controller mapeia `Result`→HTTP (presenter "inline"). Em CA estrita seria um objeto separado. |

O ponto-chave do diagrama — **o Controller não chama o Interactor diretamente, mas
através de uma fronteira polimórfica** — está satisfeito: o controller depende de
`ISender` (abstração), nunca da classe concreta do handler. O fluxo de controle vai
para fora (handler→controller), mas a dependência de código aponta para dentro. ✅

Sobre o **Presenter**: a única diferença em relação ao diagrama estrito é que o projeto
não tem uma classe Presenter separada — o mapeamento `Result → IActionResult` está no
próprio controller. Isso é uma simplificação amplamente aceita em APIs REST .NET e **não
viola a Regra de Dependência** (nada interno depende do formato de saída).

---

## 6. Pontos de atenção (não são violações)

1. **Ports de ACL em `Domain.Ports` (e não em `Application`).**
   Interfaces como `IClienteInfoPort` estão no Domain. Na Clean Architecture *estrita*,
   a interface consumida por um use case costuma viver na camada de Application. Aqui a
   escolha segue o estilo **Hexagonal** (ports como parte do núcleo). Como o Domain
   continua sem dependência de tecnologia e a implementação fica fora (adapter na
   Infrastructure), a Regra de Dependência **continua intacta**. É decisão de estilo, não
   defeito. Vale apenas documentar a convenção para manter consistência.

2. **Módulo `Autenticacao` sem camada Domain.**
   Possui só `Application`, `Infrastructure` e `Presentation` — não tem `Domain` nem
   `Contracts`. Coerente: o README registra que é login/emissão de JWT "sem entidades de
   domínio complexas". Não há regra empresarial a proteger, então o anel amarelo
   legitimamente não existe ali. Apenas assimetria consciente em relação aos outros BCs.

3. **Presenter acoplado ao Controller.**
   Conforme §5: aceitável para REST, mas se um dia for preciso múltiplos formatos de
   saída (ex.: gRPC + REST sobre o mesmo use case) valeria extrair Presenters.

4. **`Contracts` faz parte do anel verde, não é uma 5ª camada arquitetural.**
   Embora seja um projeto físico separado (para fronteira de microsserviço futura),
   conceitualmente é um **Interface Adapter** de fronteira entre contextos. Nenhum
   problema — só convém ter clareza de que não é um anel novo da Clean Architecture.

---

## 7. Conclusão

| Anel | Aderência | Observação |
|---|---|---|
| 🟡 Domain (Entities) | **Total** | Agregado rico, puro, invariantes protegidas, zero framework. |
| 🔴 Application (Use Cases) | **Total** | CQRS, depende só de abstrações, delega negócio ao Domain. |
| 🟢 Interface Adapters | **Total** | Controllers finos, repositórios e ACL adapters implementam ports internas. |
| 🔵 Frameworks & Drivers | **Total** | Frameworks confinados ao Bootstrap/Infra; núcleo agnóstico. |
| **Regra de Dependência** | **Forçada em compile-time** | `ProjectReference` impede violação; comprovada por inspeção dos `.csproj`. |

**Sua avaliação está correta: o projeto é aderente à Clean Architecture** — e de forma
acima da média, porque a Regra de Dependência não é apenas seguida, é *garantida pelo
compilador*, e o projeto combina CA com DDD tático, Ports & Adapters e fronteiras de
Bounded Context físicas. As ressalvas da §6 são de nomenclatura/estilo e não comprometem
nenhum princípio.

---

## 8. Addendum (2026-07-02) — Refatoração: artefatos nomeados do anel Interface Adapters

> A ressalva da §6 item 3 ("Presenter acoplado ao Controller") e a ausência de um
> **Gateway** físico foram endereçadas por uma 2ª fase de refatoração, cuja fonte de
> verdade é [`docs/arquitetura/refatoracao-clean-architecture/00-referencia.md`](refatoracao-clean-architecture/00-referencia.md).
> Este addendum resume o estado final; o documento de referência traz o porquê completo
> de cada decisão.

### 8.1 O que mudou

O anel verde (**Interface Adapters**) ganhou **assembly próprio por módulo**
(`{Module}.Adapters`), antes "esfregado" entre `Presentation` e `Infrastructure`. Os
três artefatos que o diagrama de Martin nomeia — **Controller, Gateway, Presenter** —
agora existem como classes físicas, não mais implícitas dentro do Handler/Controller:

```
Endpoint → {Entity}ApiController (Web, MVC)        [azul]
         → {Entity}Controller (Adapters, POCO)      [verde] — monta o Command, chama ISender.Send
         → {Operacao}Handler (Application)          [vermelho] — Use Case Interactor, devolve a ENTIDADE
         → {Entity}Gateway : I{Entity}Gateway (Adapters) → I{Entity}Repository (DataSource)
         → {Entity}Repository (Infrastructure, EF)   [azul]
   saída: Handler → {Entity}Presenter.Present(entidade) → ViewModel → {Entity}ApiController → status HTTP
```

### 8.2 Mapa de projetos → anéis (estado atual, todos os 4 módulos)

| Anel | Projeto |
|---|---|
| 🟡 Entities | `{Module}.Domain` |
| 🔴 Use Cases | `{Module}.Application` (Handlers, `Gateways/I{X}Gateway`) |
| 🟢 Interface Adapters | `{Module}.Adapters` (`Controllers/`, `Gateways/`, `Presenters/`, `DataSources/I{Entity}Repository`, `Models/`) + `{Module}.Contracts` |
| 🔵 Frameworks & Drivers | `{Module}.Infrastructure` (EF) + `{Module}.Web` (ex-`Presentation`) + `Bootstrap/Api` |

`{Module}.Web` referencia **apenas** `{Module}.Adapters` (não mais `Application`
diretamente) — o MVC `*ApiController` ficou fino: recebe HTTP, chama o Controller CA,
traduz `Result` em status HTTP. `{Module}.Adapters` não referencia ASP.NET nem EF/Npgsql
(garantido em compile-time, auditado nos `.csproj` de todos os módulos).

### 8.3 Por que o MediatR permanece (decisão consciente)

O `ISender.Send` continua sendo usado — e é o **único seam de framework** dentro do
Controller CA (anel verde). Não foi removido porque CQRS + pipeline behaviors
(`Validation`, `Logging`, `Transaction`) são pilar consciente do projeto, e o Handler
`IRequestHandler<,>` já desempenha honestamente o papel de **Use Case Interactor**: um
por operação, delegando a decisão de negócio ao agregado. O problema apontado
originalmente não era o MediatR em si, mas a **ausência de artefatos nomeados**
(Gateway, Presenter) — resolvida pela §8.1, sem trocar o mecanismo de despacho. Detalhe
completo da análise de risco/mitigação em `00-referencia.md` §3.1.

### 8.4 Módulo `Autenticacao`

Segue o mesmo desenho por consistência — ganhou `Autenticacao.Adapters` (Controller CA +
Presenter) e `Autenticacao.Presentation` foi renomeado para `Autenticacao.Web` — mesmo
sem `Domain` nem Gateway de persistência (não há agregado nem repositório: a operação de
`Login` apenas confere credenciais contra configuração e emite um JWT via
`IJwtTokenService`, um *service port* análogo a um gateway de saída).

### 8.5 O que **não** mudou

Contratos HTTP (shape do JSON) e contratos cross-module (`Contracts`) permanecem
idênticos — refatoração estrutural, nenhuma regra de negócio ou rota alterada. A
ressalva da §6 item 1 (ports de ACL) também foi endereçada: migraram de `Domain.Ports`
para `Application/Gateways` (consumidor) e `Adapters/Gateways` (implementação),
eliminando a assimetria com a Clean Architecture estrita.
