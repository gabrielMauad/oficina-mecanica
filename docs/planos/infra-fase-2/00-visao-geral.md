# Plano de Infraestrutura — Tech Challenge Fase 2 (documento âncora)

> **Para quem vai executar este plano (sessão futura do Claude Code):** este é o documento de
> entrada. Leia-o inteiro antes de tocar em qualquer arquivo. Ele contém os fatos do projeto,
> as decisões já tomadas (não as re-discuta), a ordem de leitura dos demais specs e o
> critério de pronto. Os outros documentos (`01`…`05`) são autocontidos e referenciam os
> fatos definidos aqui.

## 0. Como usar este plano

Ordem de leitura e execução:

1. **`00-visao-geral.md`** (este) — contexto, fatos, decisões, layout final, rastreabilidade.
2. **`01-docker.md`** — revisar Dockerfile e docker-compose (base para tudo).
3. **`02-kubernetes.md`** — criar todos os manifestos em `/k8s`.
4. **`03-terraform.md`** — criar `/infra` (cluster kind + aplicar manifestos via Terraform).
5. **`04-cicd.md`** — pipeline GitHub Actions (build → teste → imagem → deploy).

Cada spec tem, no fim, uma seção **"Como validar"** com comandos concretos. Rode-os — não
considere uma etapa pronta sem validar localmente.

> **Escopo deste plano:** APENAS a infraestrutura — conteinerização (Docker), orquestração
> (Kubernetes), Infraestrutura como Código (Terraform) e CI/CD. Entregáveis de documentação/
> apresentação (README, diagrama, collection de APIs, vídeo, PDF do portal) **estão fora deste
> plano**. A única "documentação" incluída é o `infra/README.md`, porque documentar os recursos
> criados e como aplicá-los é parte do próprio requisito de IaC.

## 1. Contexto

Evolução da Fase 1 de um sistema de gestão de oficina mecânica. A aplicação já foi refatorada
(Clean Architecture) e as APIs da Fase 2 já estão implementadas. **Este plano cobre apenas a
parte de INFRAESTRUTURA** dos requisitos da Fase 2: Docker, Kubernetes, Terraform (IaC) e CI/CD.

O enunciado oficial está em `docs/spec/14SOAT - Fase 2 - Tech challenge.pdf`. O material de
estudo consolidado das aulas está em `docs/aulas/consolidado_*.md` (00 requisitos, 01 terraform,
02 kubernetes, 03 docker, 04 banco, 05 cicd, 06 checklist). Perguntas/respostas oficiais do
fórum sobre infra estão em `docs/perguntas-infra.md`. **Consulte-os quando precisar de detalhe
conceitual** — este plano assume o que está lá.

## 2. Fatos do projeto (verificados no código — não presuma nada diferente)

| Fato | Valor |
|---|---|
| Raiz do repositório git | `oficina-mecanica-v2/` (todos os caminhos deste plano são relativos a ela) |
| Stack | .NET 10 (monólito modular) |
| Solution | `OficinaMecanica.slnx` (na raiz) |
| Projeto de entrada (API) | `src/Bootstrap/Api/Api.csproj` → gera `Api.dll` |
| Dockerfile existente | `src/Bootstrap/Api/Dockerfile` (multi-stage `sdk:10.0` → `aspnet:10.0`); **contexto de build = raiz do repo** |
| Banco de dados | **PostgreSQL** (Npgsql, EF Core). NÃO é MySQL. Database `oficina_mecanica` |
| Migrations | Rodam **automaticamente no startup** da API (`MigrateAsync` no `Program.cs`). Não há job de migração separado |
| Endpoint de health | `GET /healthz` (mapeado **sempre**, em qualquer ambiente) — usar nas probes |
| Docs de API (collection) | Scalar em `/scalar` e OpenAPI em `/openapi` — **só expostos quando `ASPNETCORE_ENVIRONMENT=Development`** |
| Porta HTTP | definir via `ASPNETCORE_URLS`. Este plano padroniza **8080** |
| Event bus | In-memory (`InMemoryIntegrationEventBus`) — **não há Kafka/RabbitMQ** para provisionar |
| Notificação por e-mail | **Simulada** (escreve no log via `ILogger`). Não há SMTP/token externo real |
| CI existente | `.github/workflows/ci.yml` — build + test (setup-dotnet 10.0.x, `dotnet restore/build/test OficinaMecanica.slnx`) em PR/push para `main` |
| docker-compose existente | `docker-compose.yml` na raiz (serviços `api` + `postgres:16`) |

### 2.1 Variáveis de ambiente da aplicação (convenção .NET com duplo underscore `__`)

A API lê configuração por env vars. Mapeamento definido para K8s:

| Variável | Sensível? | Vai para | Valor |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Não | ConfigMap | `Development` (ver nota abaixo) |
| `ASPNETCORE_URLS` | Não | ConfigMap | `http://+:8080` |
| `Auth__AdminEmail` | Não | ConfigMap | `admin@oficina.com` |
| `ConnectionStrings__Default` | **Sim** (contém senha) | **Secret** | `Host=oficina-postgres;Port=5432;Username=oficina;Password=<senha>;Database=oficina_mecanica;SSL Mode=Disable` |
| `Jwt__Secret` | **Sim** | **Secret** | string ≥ 32 chars |
| `Auth__AdminSenha` | **Sim** | **Secret** | senha do admin |

> **Nota sobre `ASPNETCORE_ENVIRONMENT=Development`:** manter `Development` no cluster é
> **intencional** — é o que expõe o Scalar (`/scalar`) e o OpenAPI (`/openapi`), necessários
> para o link da collection de APIs (entregável do README) e para demonstrar o consumo de APIs
> no vídeo. O `/healthz` funciona em qualquer ambiente, então as probes não dependem disso.
> Se optar por `Production`, será preciso expor o OpenAPI de outra forma — não recomendado
> para o prazo.

> **Sobre "tokens de serviços externos" (texto do requisito de Secret):** a app hoje não tem
> token externo real (e-mail é simulado). O requisito é ilustrativo e está **plenamente
> atendido** pela Secret guardando `ConnectionStrings__Default`, `Jwt__Secret` e
> `Auth__AdminSenha`. Se um dia um token real (ex.: SMTP) for adicionado, ele entra na
> **mesma** Secret.

## 3. Decisões de arquitetura (JÁ TOMADAS — não re-discutir)

Estas decisões foram fechadas com base no prazo (~4 dias), na inexperiência com AWS e nas
respostas do fórum (`docs/perguntas-infra.md`). São premissas do plano:

1. **Cluster: kind (Kubernetes in Docker), 100% local**, provisionado por Terraform com o
   provider `tehcyx/kind`. Sem cloud, sem EKS. O fórum aceita projeto totalmente local com o
   mesmo peso de nota.
2. **Banco: PostgreSQL como container dentro do cluster** (Deployment + PVC + Service), com
   volume persistente. Sem RDS. O fórum aceita banco in-cluster com o mesmo peso.
3. **CI/CD: GitHub Actions com cluster kind efêmero criado dentro do runner GitHub-hosted**
   (não self-hosted). O cluster vive dentro do runner, então não há problema de rede; o
   histórico do Actions fica autocontido (é o que os professores avaliam).
4. **Terraform aplica os manifestos com resources de verdade** (`kubectl_manifest`,
   `helm_release`) — **nunca** `local-exec` chamando `kubectl` por fora (regra explícita do
   professor no fórum).
5. **HPA só na aplicação** (no banco é opcional e não será feito).
6. **Imagem Docker publicada no Docker Hub (repositório público)** no CI; localmente usa-se
   `kind load docker-image`. Detalhes em `03` e `04`.

## 4. Convenções (usar em todos os manifestos)

- **Namespace:** `oficina-mecanica` (tudo vive nele, exceto metrics-server que fica em
  `kube-system`).
- **Prefixo de nomes:** `oficina-` (ex.: `oficina-api`, `oficina-postgres`).
- **Label comum:** `app.kubernetes.io/part-of: oficina-mecanica` + `app: <nome-do-recurso>`
  (o `app` é o que os `selector` de Service/Deployment/HPA usam).
- **Nome do cluster kind:** `oficina-mecanica`.
- **Repositório da imagem:** `docker.io/<DOCKERHUB_USERNAME>/oficina-mecanica-api`
  (o `<DOCKERHUB_USERNAME>` real será definido no CI; nos manifestos locais use uma variável/
  placeholder conforme `03`/`04`).

## 5. Layout final de arquivos a produzir

```
oficina-mecanica-v2/
├── docker-compose.yml            # revisado (01)
├── src/Bootstrap/Api/Dockerfile  # revisado (01)
├── .dockerignore                 # revisado (01)
├── k8s/                          # (02) — manifestos Kubernetes
│   ├── base/
│   │   ├── 00-namespace.yaml
│   │   ├── 01-configmap.yaml
│   │   └── 02-secret.yaml
│   ├── database/
│   │   ├── 10-postgres-pvc.yaml
│   │   ├── 11-postgres-deployment.yaml
│   │   └── 12-postgres-service.yaml
│   └── app/
│       ├── 20-api-deployment.yaml
│       ├── 21-api-service.yaml
│       └── 22-api-hpa.yaml
├── infra/                        # (03) — Terraform
│   ├── versions.tf
│   ├── providers.tf
│   ├── variables.tf
│   ├── main.tf                   # kind_cluster + metrics-server (helm) + kubectl_manifest
│   ├── outputs.tf
│   ├── terraform.tfvars.example
│   └── README.md                 # documentação dos recursos e como aplicar (requisito!)
├── .github/workflows/
│   ├── ci.yml                    # EXISTENTE — manter para PRs (04)
│   └── ci-cd.yml                 # NOVO — pipeline completa em push na main (04)
└── (README.md e demais docs de apresentação: FORA do escopo deste plano)
```

> **Importante:** o enunciado exige os manifestos em **`/k8s`** e o Terraform em **`/infra`**
> (na raiz do repo). Respeite esses nomes de pasta.

## 6. Rastreabilidade — requisito → onde é atendido

| Requisito do PDF | Atendido em |
|---|---|
| Dockerfile atualizado | `01-docker.md` |
| docker-compose para dev local | `01-docker.md` |
| K8s: Deployments | `02` (API + Postgres) |
| K8s: Services | `02` (API + Postgres) |
| K8s: ConfigMaps | `02` (`01-configmap.yaml`) |
| K8s: Secrets (variáveis sensíveis) | `02` (`02-secret.yaml`) |
| K8s: HPA por CPU/memória | `02` (`22-api-hpa.yaml`) + metrics-server em `03` |
| Terraform: provisionar cluster K8s (local ou cloud) | `03` (kind_cluster) |
| Terraform: Banco de Dados | `03` (aplica os manifestos do Postgres como resources) + `02` |
| Terraform: documentar recursos e como aplicar | `03` (`infra/README.md`) |
| CI/CD: build da aplicação | `04` |
| CI/CD: execução dos testes | `04` (reaproveita `ci.yml`) |
| CI/CD: build da imagem Docker | `04` (Docker Hub) |
| CI/CD: deploy no cluster + deploy do banco + aplicar manifestos | `04` (terraform apply em kind efêmero) |

> Os entregáveis de documentação/apresentação do PDF (README, diagrama, collection, vídeo, PDF
> do portal, compartilhar repo com `soat-architecture`) **não fazem parte deste plano** — ver
> nota de escopo na seção 0. Checklist detalhado original: `docs/aulas/consolidado_06_checklist.md`.

## 7. Definition of Done (o plano está concluído quando)

- [ ] `docker compose up --build` sobe API + Postgres e `GET http://localhost:8080/healthz` responde 200.
- [ ] `cd infra && terraform init && terraform apply -auto-approve` cria o cluster kind e sobe
      Postgres + API + HPA; `kubectl get pods -n oficina-mecanica` mostra tudo `Running`.
- [ ] `kubectl top pods -n oficina-mecanica` retorna métricas (metrics-server OK) e
      `kubectl get hpa -n oficina-mecanica` mostra TARGETS com valor (não `<unknown>`).
- [ ] Um teste de carga faz o HPA escalar as réplicas da API para cima (demonstrável).
- [ ] O workflow `ci-cd.yml` roda no GitHub Actions **verde de ponta a ponta** (build, teste,
      imagem, deploy no kind efêmero, smoke test em `/healthz`).
- [ ] `infra/README.md` documenta cada recurso Terraform e como aplicar/destruir (requisito de IaC).

## 8. Gotchas conhecidos (que causariam "furos" se ignorados)

1. **metrics-server no kind não sobe sem a flag `--kubelet-insecure-tls`** — sem ela, o HPA
   fica com TARGETS `<unknown>` para sempre. Tratado em `03` (via helm values).
2. **A API roda migrations no startup** e falha se o Postgres ainda não estiver pronto →
   usar um **initContainer** que espera o Postgres (em `02`), senão o pod entra em
   `CrashLoopBackOff` até o banco subir.
3. **HPA exige `resources.requests.cpu` no container da API** — sem isso o HPA não calcula o
   percentual. Definido em `02`.
4. **kind não expõe NodePort no host automaticamente** — ou se usa `extra_port_mappings` no
   `kind_config` (feito em `03`) ou `kubectl port-forward` (usado nos demos). Ambos documentados.
5. **A imagem precisa estar acessível ao cluster** — Docker Hub público (CI) ou `kind load`
   (local). `imagePullPolicy: IfNotPresent`. Detalhado em `03`/`04`.
6. **Banco e app precisam de credenciais consistentes** — o `POSTGRES_USER`/`POSTGRES_PASSWORD`
   do Postgres e o `ConnectionStrings__Default` da API saem da **mesma Secret** e têm de bater.
