# Sistema de Oficina Mecânica

Back-end de um sistema integrado de atendimento e execução de serviços para oficina mecânica,
desenvolvido como **Tech Challenge da pós-graduação em Arquitetura de Software (FIAP/SOAT)**.

- **Fase 1** — back-end monolítico com DDD, APIs REST, autenticação JWT e testes.
- **Fase 2** — **evolução da aplicação** (refatoração para **Clean Code + Clean Architecture** e
  novas APIs de Ordem de Serviço) **e infraestrutura** (Docker, **Kubernetes**, **Terraform**,
  **CI/CD** e escalabilidade automática com HPA).

> 📚 **Toda a documentação detalhada está em [`docs/`](docs/README.md)** — este README concentra
> o contexto e os links. Comece por [`docs/README.md`](docs/README.md) para navegar.

---

## Índice

- [Sobre o Projeto](#sobre-o-projeto)
- [Arquitetura e Desenhos da Solução](#arquitetura-e-desenhos-da-solução)
- [Execução Local (Docker Compose)](#execução-local-docker-compose)
- [Deploy em Kubernetes](#deploy-em-kubernetes)
- [Provisionamento da Infraestrutura (Terraform)](#provisionamento-da-infraestrutura-terraform)
- [CI/CD](#cicd)
- [Autenticação](#autenticação)
- [APIs — Documentação e Collection](#apis--documentação-e-collection)
- [Testes](#testes)
- [Vídeo Demonstrativo](#vídeo-demonstrativo)
- [Documentação Completa](#documentação-completa)

---

## Sobre o Projeto

### O Desafio

Construir um back-end de gestão de oficina com **DDD aplicado**, **APIs RESTful documentadas**,
**autenticação JWT**, validação de CPF/CNPJ e placa, **cobertura de testes ≥ 80%** nos domínios
críticos e orquestração via Docker (Fase 1) — e, na Fase 2, provisionar e implantar toda a
solução em **Kubernetes com IaC e CI/CD**.

### Objetivos da Fase 2

A Fase 2 evolui a Fase 1 em **dois pilares** — qualidade/organização do código **e**
infraestrutura escalável e automatizada.

**Pilar 1 — Evolução da aplicação**

| Objetivo | Entregue |
|---|---|
| **Clean Code + Clean Architecture** | Refatoração da Fase 1: separação de camadas e dependências com artefatos nomeados (Controller CA, Gateway, Presenter, Use Cases). Planos em [`docs/planos/refatoracao-clean-architecture/`](docs/planos/refatoracao-clean-architecture/) |
| **Novas/alteradas APIs de OS** | Abertura completa da OS (cliente, veículo, serviços e peças de uma vez), consulta de status, aprovação/recusa de orçamento, **listagem ordenada por status** com **exclusão lógica** de OS finalizadas/entregues, e notificação de status |
| **Testes automatizados** | Unitários (xUnit) + integração (Testcontainers) cobrindo os fluxos críticos, cobertura ≥ 80% nos domínios |

**Pilar 2 — Infraestrutura e automação**

| Objetivo | Entregue |
|---|---|
| **Conteinerização** | `Dockerfile` multi-stage + `docker-compose` para dev local |
| **Orquestração** em Kubernetes | Deployments, Services, ConfigMap, Secret e **HPA** em [`k8s/`](k8s/) |
| **Escalabilidade automática** | HorizontalPodAutoscaler por CPU (min 1 / max 5) |
| **Infraestrutura como Código** | Módulo **Terraform** em [`infra/`](infra/) provisiona cluster + banco + app |
| **CI/CD** | Pipeline GitHub Actions: build → teste → imagem → deploy → smoke test |

> Detalhes das decisões de Clean Architecture em
> [`docs/arquitetura/clean-architecture.md`](docs/arquitetura/clean-architecture.md) e das mudanças
> funcionais da OS em [`docs/arquitetura/decisoes.md`](docs/arquitetura/decisoes.md).

### Objetivos Adicionais (estudo de arquitetura)

Além do MVP exigido, o projeto incorpora deliberadamente práticas avançadas:

- **Modular Monolith** com fronteiras de Bounded Context físicas (assembly por camada por
  módulo), preparando a extração futura para microsserviços com mínimo retrabalho.
- **DDD real**: domain events existem apenas quando há consumidor concreto.
- **CQRS com MediatR v12**: vertical slices, pipeline behaviors (validação, logging, transação).
- **Integration Events in-process**: bus desacoplado (`IIntegrationEventBus`) substituível por
  RabbitMQ/Kafka sem alterar Application ou Domain.
- **Anti-Corruption Layer (ACL)** para comunicação síncrona entre módulos via ports e adapters.

---

## Arquitetura e Desenhos da Solução

Os três desenhos exigidos na Fase 2 estão em [`docs/arquitetura/diagramas/`](docs/arquitetura/diagramas/)
(Mermaid — renderizam direto no GitHub):

| Desenho | Arquivo |
|---|---|
| 🧩 **Componentes da aplicação** (C4 níveis 1–3) | [`diagramas/componentes.md`](docs/arquitetura/diagramas/componentes.md) |
| 🏗️ **Infraestrutura provisionada** (cluster kind, banco, API, HPA) | [`diagramas/infraestrutura.md`](docs/arquitetura/diagramas/infraestrutura.md) |
| 🚀 **Fluxo de deploy** (CI/CD) | [`diagramas/fluxo-deploy.md`](docs/arquitetura/diagramas/fluxo-deploy.md) |

### Modular Monolith + Clean Architecture

O projeto é aderente à **Clean Architecture** (os quatro anéis são projetos físicos distintos e
a Regra de Dependência é forçada em compile-time) e organizado como **Modular Monolith**: cada
Bounded Context tem 5 projetos próprios, com a fronteira enforçada por referências de projeto.
Para extrair um microsserviço, basta mover os projetos do módulo, trocar adapters por HTTP
clients e o bus in-process por mensageria — nada no Domain ou Application muda.

### Bounded Contexts

| Módulo | Responsabilidade |
|---|---|
| **Autenticacao** | Login e emissão de JWT |
| **Cadastro** | Cliente, Veículo, Serviço (catálogo) |
| **PecasInsumos** | Estoque de peças, disponibilidade, entradas e saídas |
| **OrdemServico** | Ciclo de vida completo da OS e orçamento |

### Banco de Dados

**PostgreSQL 16**, banco único (`oficina_mecanica`), **1 schema por módulo** (`cadastro`,
`pecas_insumos`, `ordem_servico`), **sem FK cross-schema** — o isolamento simula microsserviços.

> 📖 Aprofundamento: [`estrutura-do-projeto.md`](docs/arquitetura/estrutura-do-projeto.md) ·
> [`clean-architecture.md`](docs/arquitetura/clean-architecture.md) ·
> [`decisoes.md`](docs/arquitetura/decisoes.md) ·
> [`database-schema.md`](docs/arquitetura/database-schema.md) ·
> [`event-storming.md`](docs/arquitetura/event-storming.md).

---

## Execução Local (Docker Compose)

Forma mais rápida de subir tudo para desenvolvimento e testes manuais.

**Pré-requisitos:** Docker + Docker Compose v2.x. (.NET SDK 10.0 apenas para rodar testes fora do
container.)

```bash
git clone <repo>
cd oficina-mecanica-v2

docker compose up --build
```

Sobe dois serviços:
- **`postgres`** — PostgreSQL 16, banco `oficina_mecanica`, porta `5432`
- **`api`** — aplicação .NET 10, porta `8080`

As **migrations são aplicadas automaticamente** na inicialização da API (`MigrateAsync()` em
`Program.cs`). Nenhum comando manual é necessário.

### Pontos de acesso

| Serviço | URL |
|---|---|
| API REST | `http://localhost:8080/api/v1/` |
| Scalar (docs interativa) | `http://localhost:8080/scalar` |
| Health check | `http://localhost:8080/healthz` |
| PostgreSQL | `localhost:5432` — user/pass: `oficina` / `oficina-dev-pass` |

---

## Deploy em Kubernetes

Os manifestos estão em [`k8s/`](k8s/), organizados em `base/` (namespace, ConfigMap, Secret),
`database/` (PVC, Deployment e Service do PostgreSQL) e `app/` (Deployment, Service NodePort e
**HPA** da API). O deploy é feito de forma **declarativa via Terraform** (que aplica esses
manifestos como _resources_) — ver a seção seguinte.

Recursos-chave do cluster:

- **Cluster local:** [kind](https://kind.sigs.k8s.io/) (Kubernetes in Docker), namespace
  `oficina-mecanica`.
- **HPA:** `oficina-api-hpa` escala a API de **1 a 5 réplicas** ao ultrapassar **50% de CPU**
  (depende do metrics-server, provisionado junto).
- **Acesso:** `http://localhost:30080` (NodePort) ou
  `kubectl port-forward -n oficina-mecanica svc/oficina-api 8080:8080`.

Comandos úteis após o deploy:

```bash
kubectl get pods,svc,hpa -n oficina-mecanica
kubectl top pods -n oficina-mecanica          # métricas (metrics-server)
kubectl get hpa -n oficina-mecanica -w        # acompanhar a escalabilidade
```

> Desenho do cluster: [`diagramas/infraestrutura.md`](docs/arquitetura/diagramas/infraestrutura.md).

---

## Provisionamento da Infraestrutura (Terraform)

O módulo em [`infra/`](infra/) provisiona **tudo** de forma declarativa — cluster kind,
metrics-server (via Helm) e todos os manifestos do `k8s/` — usando _resources_ Terraform de
verdade (`kind_cluster`, `helm_release`, `kubectl_manifest`), **sem `local-exec`**.

**Pré-requisitos:** Docker, Terraform ≥ 1.5, `kind` e `kubectl` no PATH.

```bash
cd infra
terraform init

# 1. cria só o cluster kind primeiro
terraform apply -auto-approve -target=kind_cluster.this

# 2. builda a imagem (da raiz do repo) e carrega no cluster
cd ..
docker build -f src/Bootstrap/Api/Dockerfile -t oficina-mecanica-api:local .
kind load docker-image oficina-mecanica-api:local --name oficina-mecanica

# 3. aplica o resto (metrics-server, base, banco, app, HPA)
cd infra
terraform apply -auto-approve
```

Para destruir tudo (remove o cluster kind e todo o conteúdo):

```bash
cd infra && terraform destroy -auto-approve
```

> 📖 Passo a passo completo (local e CI), tabela de recursos e troubleshooting em
> [`infra/README.md`](infra/README.md).

---

## CI/CD

Dois workflows do GitHub Actions:

| Workflow | Gatilho | O que faz |
|---|---|---|
| [`ci.yml`](.github/workflows/ci.yml) | Pull Request → `main` | build + testes (validação de PR) |
| [`ci-cd.yml`](.github/workflows/ci-cd.yml) | Push/merge → `main` | build → teste → imagem (Docker Hub) → **deploy em cluster kind efêmero** → smoke test em `/healthz` → destroy |

O cluster kind é criado **dentro do runner** GitHub-hosted, então o histórico do Actions é
autocontido. Segredos necessários: `DOCKERHUB_USERNAME` e `DOCKERHUB_TOKEN`.

> Desenho do pipeline: [`diagramas/fluxo-deploy.md`](docs/arquitetura/diagramas/fluxo-deploy.md).

---

## Autenticação

A maioria dos endpoints requer JWT. Para obtê-lo:

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@oficina.com", "senha": "admin123"}'
```

O token tem validade de **1 hora**. Use-o como `Authorization: Bearer <token>` ou pelo botão
**Authorize** no Scalar.

> Credenciais e segredo JWT são definidos no `docker-compose.yml` (dev) e na Secret do Kubernetes
> (cluster). Em produção, substitua `Jwt__Secret`, `Auth__AdminEmail` e `Auth__AdminSenha`.

**Endpoints públicos** (sem autenticação): consulta de acompanhamento da OS pelo cliente e
`GET /healthz`.

---

## APIs — Documentação e Collection

| Recurso | Onde |
|---|---|
| **Documentação interativa (Scalar/OpenAPI)** | `http://localhost:8080/scalar` (e `/openapi`) com a API rodando |
| **Collection completa (Bruno)** | [`docs/guias/collection_bruno.yml`](docs/guias/collection_bruno.yml) — importável no [Bruno](https://usebruno.com), ambiente `Local` pré-configurado |

A collection Bruno inclui todos os módulos e endpoints, incluindo os novos da Fase 2.

---

## Testes

### Guias E2E

- **[Cenário feliz (happy path)](docs/guias/teste-cenario-feliz.md)** — fluxo completo com
  exemplos `curl`, resultados esperados e checklist.
- **[Cenários alternativos](docs/guias/teste-cenarios-alternativos.md)** — validações de erro:
  CPF/CNPJ inválido, placa inválida, transições inválidas, estoque insuficiente, rejeição + estorno.

### Testes automatizados

```bash
# Todos (unitários + integração)
dotnet test OficinaMecanica.slnx

# Apenas unitários (sem Docker)
dotnet test OficinaMecanica.slnx --filter "Category!=Integration"

# Apenas integração (requer Docker — Testcontainers.PostgreSql)
dotnet test tests/IntegrationTests
```

| Camada | Ferramenta |
|---|---|
| Framework | xUnit |
| Mocks | Moq |
| Integração (banco real) | Testcontainers.PostgreSql |
| Cobertura | Coverlet + ReportGenerator |

**Domain.Tests** — puros, sem IO, sustentam a cobertura ≥ 80%. **IntegrationTests** — sobem a
aplicação completa com `WebApplicationFactory<Program>` e Postgres real, validando migrations,
adapters e o pipeline de eventos de ponta a ponta.

### Cobertura (meta ≥ 80%)

```bash
dotnet test OficinaMecanica.slnx --collect:"XPlat Code Coverage" --results-directory coverage-results/
reportgenerator -reports:"coverage-results/**/coverage.cobertura.xml" -targetdir:"coverage-report/" -reporttypes:Html
```

![Coverage Summary](docs/images/coverage-summary.png)
![Coverage Detail](docs/images/coverage-detail.png)

---

## Vídeo Demonstrativo

> 🎥 **[Assista à demonstração no YouTube](ADICIONAR_LINK_DO_VIDEO)** _(link a preencher)_

O vídeo (≤ 15 min, público ou não listado) demonstra:

- Deploy da aplicação (Terraform provisiona o cluster e sobe a stack)
- Execução do CI/CD (pipeline no GitHub Actions)
- Consumo das APIs (via Scalar / Bruno)
- **Escalabilidade automática** — carga na API dispara o HPA e escala as réplicas

---

## Documentação Completa

Índice navegável de toda a documentação: **[`docs/README.md`](docs/README.md)**.

| Tema | Pasta |
|---|---|
| Arquitetura, diagramas e decisões | [`docs/arquitetura/`](docs/arquitetura/) |
| Planos de implementação e infra | [`docs/planos/`](docs/planos/) |
| Guias de teste e collection | [`docs/guias/`](docs/guias/) |
| Recursos Terraform (passo a passo) | [`infra/README.md`](infra/README.md) |
| Enunciados oficiais (FIAP) | [`docs/spec/`](docs/spec/) |

---

## Licença

Ver [`LICENSE`](LICENSE).
