# Desenho — Componentes da Aplicação

> Modelo C4 (níveis 1 → 3) do **Sistema de Oficina Mecânica**, um _Modular Monolith_ em
> .NET 10 com quatro Bounded Contexts e aderência à Clean Architecture.
> Diagramas em [Mermaid](https://mermaid.js.org/) — renderizam nativamente no GitHub.
>
> Documentação de apoio: [`estrutura-do-projeto.md`](../estrutura-do-projeto.md),
> [`clean-architecture.md`](../clean-architecture.md).

---

## Nível 1 — Contexto

Quem usa o sistema e com o que ele fala. O sistema é um back-end único (não há integrações
externas reais: e-mail é simulado e o barramento de eventos é in-process).

```mermaid
flowchart TB
    atendente["Atendente / Mecânico"]
    cliente["Cliente"]
    sistema["Sistema de Oficina Mecânica<br/>Back-end REST .NET 10"]
    db[("PostgreSQL 16<br/>1 schema por módulo")]

    atendente -->|"HTTPS/JSON (JWT)"| sistema
    cliente -->|"consulta pública da OS"| sistema
    sistema -->|"EF Core / Npgsql"| db

    classDef person fill:#08427b,stroke:#052e56,color:#fff
    classDef system fill:#1168bd,stroke:#0b4884,color:#fff
    classDef store fill:#438dd5,stroke:#2e6295,color:#fff
    class atendente,cliente person
    class sistema system
    class db store
```

---

## Nível 2 — Containers (módulos)

O monólito roda em **um único container** (`Bootstrap/Api`), mas internamente é dividido em
quatro Bounded Contexts com fronteiras **físicas** (assembly próprio por camada por módulo) —
não em camadas horizontais. O `SharedKernel` fornece os tipos-base e o pipeline transversal.

![Diagrama C4 de Containers](../../images/C4%20-%20Containers.png)

**Comunicação entre módulos** (nenhum módulo referencia Domain/Application de outro):

| Tipo | Quando | Mecanismo |
|---|---|---|
| **Síncrona (ACL)** | precisa de resposta imediata (ex.: cliente existe ao abrir OS) | _port_ no Domain do consumidor + _adapter_ na Infrastructure consumindo os `Contracts` do produtor |
| **Assíncrona (Integration Events)** | fato consumado que outro BC reage (ex.: orçamento gerado → baixa estoque) | evento em `<Modulo>.Contracts` publicado no `IIntegrationEventBus` após o commit |

---

## Nível 3 — Componentes de um módulo (Clean Architecture)

Recorte do módulo **OrdemServico** (o mais completo). Cada anel da Clean Architecture é um
projeto físico; a Regra de Dependência é forçada em compile-time — as setas apontam sempre
para dentro.

![Diagrama C4 de Componentes](../../images/C4%20-%20Componentes.png)

**Persistência sem acoplar o Domain (DTOs de persistência):** o EF Core mapeia **Records**
(`OrdemServicoRecord`, etc., em `Adapters/DataSources/Records`), **nunca o agregado de Domain**.
O `DbContext`, as `Configurations` e o `Repository` da Infrastructure só conhecem Records; o
`Mapper` (`Adapters/DataSources/Mappers`) converte Record ↔ Domain dentro do Gateway. Assim o
Domain não tem nenhuma referência a EF/Npgsql. _(refatoração da Fase 2 — ver
[`../../planos/refatoracao-clean-architecture/07-plano-dtos-persistencia.md`](../../planos/refatoracao-clean-architecture/07-plano-dtos-persistencia.md).)_

**Por que Contracts é um pacote à parte (fora dos 4 anéis):** `OrdemServico.Contracts` é a
**interface pública publicada do módulo** — queries síncronas, DTOs e integration events que
**outros** Bounded Contexts consomem. Não é Domain (não tem entidades), nem Application (não tem
use cases), nem Adapters/Infrastructure. Depende **apenas de `SharedKernel.Domain`** e por isso é
um projeto separado, referenciado tanto por este módulo (a Application publica os integration
events; a Infrastructure implementa as queries) quanto pelos módulos consumidores. É o
equivalente à "linguagem publicada" do BC.

**Regra de Dependência** — referências permitidas por camada:

| Camada | Depende de |
|---|---|
| **Domain** | `SharedKernel.Domain` apenas |
| **Application** | Domain, SharedKernel.*, Contracts (próprios e de outros módulos) |
| **Adapters** | Application, Domain, Contracts, MediatR — **sem ASP.NET/EF** |
| **Contracts** | `SharedKernel.Domain` apenas |
| **Infrastructure** | Application, Domain, Contracts, Adapters, EF/Npgsql |
| **Web** | Adapters, SharedKernel.* |

> Detalhamento em [`estrutura-do-projeto.md`](../estrutura-do-projeto.md) e
> [`clean-architecture.md`](../clean-architecture.md).
