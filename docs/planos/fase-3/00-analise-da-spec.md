# Fase 3 — Análise da spec vs. estado atual

> Fonte: `docs/spec/13SOAT - Fase 3 - Tech Challenge.pdf` (texto extraído e interpretado integralmente).
> Data da análise: 2026-09-07. Estado do repo analisado: branch `main`, commit `37a08bf`.

---

## 1. O que a spec exige (leitura literal, sem furos)

### 1.1 Autenticação e API Gateway
1. Implementar um **API Gateway** (AWS API Gateway, Kong, Traefik ou outro).
2. **Proteger rotas sensíveis** da aplicação com autenticação **via CPF**.
3. Criar uma **Function Serverless** que:
   - valida o CPF do cliente;
   - consulta **existência e status** do cliente **na base de dados**;
   - gera e devolve um **JWT válido** para consumo das APIs protegidas.

> Implicação não escrita, mas obrigatória: a aplicação em Kubernetes tem que **aceitar/validar
> o JWT emitido pela Lambda** (mesmo segredo/chave, mesmo issuer/audience). Hoje ela valida um JWT
> emitido por ela mesma — são dois emissores diferentes.

### 1.2 Estrutura de repositórios e CI/CD
4. **Quatro repositórios separados**, cada um com CI/CD e **deploy automático para a nuvem**:
   1. Lambda (function serverless);
   2. Infraestrutura Kubernetes (Terraform);
   3. Infraestrutura do Banco de Dados Gerenciado (Terraform);
   4. Aplicação principal executando em Kubernetes.
5. Regras de proteção: **`main` protegida** (sem commit direto), **PR obrigatório** para merge,
   **deploy automático das branches de homologação e produção**.

> Implicação: é preciso existir uma branch de **homologação** (`develop`/`homolog`/`staging`) além da
> de produção, e **dois ambientes** (ou ao menos dois targets de deploy) na nuvem.

### 1.3 Infraestrutura obrigatória (nuvem livre)
6. API Gateway para controle e roteamento.
7. Function Serverless para autenticação.
8. **Banco de dados gerenciado** (RDS/Aurora/Cloud SQL/etc.) — não mais Postgres em pod.
9. **Cluster Kubernetes com escalabilidade** (gerenciado, ex.: EKS) — não mais kind.
10. **Terraform** para provisionamento de tudo.

### 1.4 Monitoramento e observabilidade
11. Integração com **Datadog ou New Relic** (escolha livre).
12. Monitorar: **latência das APIs**; **consumo de CPU/memória do Kubernetes**;
    **healthchecks e uptime**; **alertas para falhas no processamento de ordens de serviço**;
    **logs estruturados em JSON com correlação entre requisições** (correlation/trace id).
13. **Dashboards** com: **volume diário de OS**; **tempo médio de execução por status**
    (Diagnóstico, Execução, Finalização); **erros e falhas nas integrações**.

> Implicação: os itens de dashboard são **métricas de negócio** — a aplicação precisa emitir
> eventos/métricas customizadas (contagem de OS por dia, duração por transição de status, erros de
> integração). Não sai de graça só instalando o agente.

### 1.5 Documentação da arquitetura
14. **Diagrama de Componentes** com visão de nuvem, APIs, banco, **Kubernetes**,
    **Function Serverless** e monitoramento.
15. **Diagramas de Sequência** para (a) fluxo de autenticação e (b) abertura de ordem de serviço.
16. **RFCs** para decisões técnicas relevantes (escolha da nuvem, do banco, da estratégia de auth).
17. **ADRs** para decisões arquiteturais permanentes (padrão de comunicação, uso de HPA, etc.).
18. **Justificativa formal da escolha do banco** + **ajustes no modelo relacional**, com
    **diagrama ER** e explicação dos relacionamentos.

### 1.6 Entregáveis
19. Os 4 repositórios com código, CI/CD, **pipelines funcionais** e **links para os deploys ativos**.
    **Dockerfile só onde for tecnicamente necessário** — a app em Kubernetes tem; repositórios
    compostos apenas de Terraform **não precisam** de Dockerfile.
20. **README.md em cada repositório** com **7 itens**: propósito, tecnologias utilizadas,
    **pré-requisitos**, instruções de execução, passos de deploy, **explicação da pipeline**,
    **diagrama do componente daquele repositório** e **link para Swagger/Postman** (quando aplicável).
21. **Vídeo ≤ 15 min** (YouTube/Vimeo, público ou não listado) demonstrando: autenticação com CPF,
    **geração e utilização do JWT**, consumo das APIs protegidas, execução da pipeline de CI/CD,
    deploy automatizado, dashboards de monitoramento, **logs estruturados**, **correlação das
    requisições** e **traces em execução**.
22. **PDF único no Portal do Aluno** com: links dos 4 repos, link do vídeo, links das documentações
    e **confirmação do usuário `soat-architecture` adicionado a todos os repositórios**.

### 1.7 Esclarecimentos oficiais do coordenador (mensagem na thread da turma)

> O coordenador declarou que **o PDF da spec + a mensagem dele são a referência oficial** e que
> **não é necessário consultar a gravação da live**. A mensagem **não introduz requisito novo** —
> confirma as interpretações abaixo e detalha alguns pontos, já incorporados nos itens 14, 19, 20 e 21.

Pontos confirmados que estavam implícitos ou ambíguos no PDF:

- **Function Serverless**: valida o CPF, consulta existência **e status** do cliente na base e
  **só então**, se autorizado, gera o JWT — que é usado depois nas APIs protegidas. Confirma a
  necessidade de a app aceitar um token emitido por outro emissor.
- **Pipelines**: devem executar "as validações pertinentes ao componente". Para a app: compilar,
  testar, buildar imagem, publicar e deployar. Para os repos de infra: **validar e aplicar** o Terraform.
- **Dockerfile**: explicitamente **dispensado** nos repositórios só de Terraform.
- **README**: acrescenta **pré-requisitos** e **explicação da pipeline** à lista do PDF.
- **Diagrama de componentes**: acrescenta **Kubernetes** e **Function Serverless** ao escopo.
- **RFCs/ADRs**: **não há quantidade mínima** — o grupo registra o que for relevante. Sugestões
  citadas: RFC para nuvem, banco, estratégia de autenticação e ferramenta de observabilidade;
  ADR para padrão de comunicação, estratégia de escalabilidade, uso de HPA e **organização de
  logs e traces**.
- **Logs**: JSON "preferencialmente", com `correlationId` **ou** `traceId`, permitindo seguir uma
  requisição atravessando os componentes.
- **Dashboards**: precisam ser **demonstrados no vídeo**.
- **PDF do portal**: é só um índice de links — **não** deve conter código nem a documentação inteira.
- **Nuvem, banco e ferramenta de CI/CD**: escolha livre, desde que a infra tenha API Gateway,
  serverless, banco gerenciado, cluster Kubernetes com escalabilidade e Terraform.

---

## 2. Estado atual do projeto (o que já existe)

| Área | Situação hoje | Serve para a Fase 3? |
|---|---|---|
| Aplicação .NET 10, modular monolith, Clean Architecture | Completa, 4 BCs, testes unit + integração | ✅ Base reaproveitada |
| Autenticação | `POST /api/v1/auth/login` com **email + senha de admin hardcoded** (`AdminUserOptions`), JWT emitido pela própria API | ❌ Spec pede **CPF** + emissão pela **Lambda** |
| API Gateway | Não existe | ❌ Faltando |
| Function serverless | Não existe | ❌ Faltando |
| Banco | PostgreSQL 16 **em pod** (`k8s/database/`), PVC local | ❌ Precisa ser **gerenciado** |
| Kubernetes | **kind** local/efêmero, namespace `oficina-mecanica`, HPA 1–5 @50% CPU, probes ok | ⚠️ Manifestos reaproveitáveis; cluster precisa virar gerenciado |
| Terraform | `infra/` provisiona kind + metrics-server + manifests. **`terraform.tfstate` está commitado no repo** | ⚠️ Reescrever para nuvem + **state remoto** |
| CI/CD | `ci.yml` (PR → build/test) e `ci-cd.yml` (push main → imagem Docker Hub → kind efêmero no runner → smoke test → destroy) | ⚠️ Não há deploy real na nuvem, nem branch de homologação |
| Proteção de branch | `main` protegida, PR obrigatório, status check "Build & Test", sem force-push | ✅ Já atende (replicar nos 4 repos) |
| `soat-architecture` | Já é colaborador do repo atual | ✅ Replicar nos outros 3 |
| Observabilidade | **Zero** — nenhuma referência a Serilog/OpenTelemetry/Datadog/New Relic/Prometheus. Logging default do ASP.NET (texto, sem correlação) | ❌ Faltando por inteiro |
| Health check | `/healthz` (`AddHealthChecks()` sem checks registrados — só responde 200) | ⚠️ Melhorar (check de banco) |
| Documentação | Componentes (C4 1–3), infraestrutura (kind), fluxo de deploy, `database-schema.md`, event storming, decisões | ⚠️ Sem visão de nuvem, **sem diagrama de sequência**, **sem RFC**, **sem ADR**, **sem ER formal** |
| API docs | Scalar/OpenAPI (só em Development) + collection Bruno | ⚠️ Spec pede link de Swagger/Postman por repo; expor OpenAPI fora de Development |
| Repositórios | **1 monorepo** (`gabrielMauad/oficina-mecanica`) | ❌ Precisa virar 4 |
| Vídeo | Fase 2 (`youtu.be/ZnaUtgUUb5I`) | ❌ Novo vídeo para Fase 3 |

---

## 3. Divisão do trabalho

### 3.1 ☁️ O que é feito na nuvem (AWS)

> Observação importante: quase tudo abaixo **deve nascer de Terraform** (a spec exige IaC) — o
> código do Terraform mora nos repositórios (bucket 3.2). O que está listado aqui é o **recurso
> AWS em si** mais o que **precisa ser feito manualmente no console/CLI** porque é pré-requisito
> do próprio Terraform.

**Feito à mão no console/CLI (bootstrap, uma vez):**
- Conta AWS / credenciais. ⚠️ **Verificar antes de tudo** se será usada uma conta AWS Academy da
  FIAP — ela restringe criação de IAM roles/policies, tem sessão de ~4h e derruba recursos; isso
  muda a estratégia de OIDC e de deploy automático (cai para access keys de curta duração em
  secrets do GitHub).
- **Backend remoto do Terraform**: bucket S3 (+ DynamoDB para lock) — precisa existir antes do
  primeiro `terraform init`, e é o que permite os 3 repos de infra compartilharem outputs
  (`terraform_remote_state`).
- **OIDC provider + IAM Role para GitHub Actions** (se a conta permitir) — deploy sem access key
  estática. Alternativa: usuário IAM + secrets no GitHub.
- Verificar **quotas/limites** (EIPs, NAT Gateway, vCPU) e escolher região (`us-east-1`).

**Provisionado via Terraform (recursos AWS):**
- **VPC** com subnets públicas/privadas, NAT, security groups (base compartilhada pelos repos de infra).
- **Amazon RDS PostgreSQL** (banco gerenciado) — subnet group privado, SG liberando só o cluster e a
  Lambda, backup/retention, parâmetro de log. Credenciais em **Secrets Manager** ou **SSM Parameter Store**.
- **Amazon EKS** — cluster + node group gerenciado com autoscaling, **metrics-server**,
  **Cluster Autoscaler ou Karpenter** (a spec pede "cluster com escalabilidade": HPA no pod +
  autoscaling de nós), OIDC/IRSA para o service account da app ler segredos.
- **Amazon ECR** (repositório de imagem da API) — ou manter Docker Hub; ECR é mais coerente com AWS.
- **AWS Lambda** de autenticação (runtime .NET 8/10 ou Node) na VPC, com acesso ao RDS, lendo o
  segredo do JWT do Secrets Manager.
- **Amazon API Gateway** (HTTP API) — rota pública `POST /auth` → Lambda; rotas protegidas
  → integração com a app no EKS (via **VPC Link + NLB/ALB do Ingress**), com **JWT authorizer**
  (ou Lambda authorizer) validando o token emitido pela Lambda.
- **Ingress/Load Balancer** no EKS (AWS Load Balancer Controller) para o API Gateway alcançar a app.
- **CloudWatch Logs** (destino dos logs da Lambda) e, se usar Datadog/New Relic, o forwarder/agente.
- **Secrets Manager / SSM**: `Jwt__Secret` compartilhado entre Lambda e app, connection string do RDS,
  chave de API do Datadog/New Relic.
- Dois ambientes (**homologação** e **produção**) — via workspaces do Terraform ou diretórios
  `envs/homolog` e `envs/prod`.

### 3.2 💻 O que é feito nos repositórios git (localmente)

**A. Split em 4 repositórios** (todos com README completo, diagrama próprio, CI/CD, `soat-architecture` como colaborador):

| # | Repo sugerido | Conteúdo |
|---|---|---|
| 1 | `oficina-mecanica-lambda-auth` | Código da function de autenticação por CPF + testes + Dockerfile (se container image) + pipeline de deploy |
| 2 | `oficina-mecanica-infra-k8s` | Terraform de VPC + EKS + node groups + addons + Ingress/ALB + API Gateway |
| 3 | `oficina-mecanica-infra-db` | Terraform do RDS PostgreSQL + subnet group + SG + secrets |
| 4 | `oficina-mecanica-app` | Aplicação .NET (este repo atual, enxugado) + `k8s/` + Dockerfile + pipeline |

> Cuidado com a ordem de dependência: **infra-db** e **infra-k8s** publicam outputs (endpoint do RDS,
> nome do cluster, ARN do API Gateway) que **lambda** e **app** consomem via `terraform_remote_state`
> ou SSM. Definir isso antes de escrever os pipelines.

**B. Aplicação (.NET)**
- **Autenticação por CPF**: novo contrato `POST /auth { cpf }`; o LoginHandler atual (email/senha admin)
  ou vira legado de admin, ou é substituído. A validação de existência/status do cliente já tem base
  (`Cadastro.Domain.Cpf`, `Documento`, campo `ativo`) — falta expor uma consulta por documento
  (hoje só existe `GET /api/v1/clientes` e `GET /clientes/{id}`).
- **Aceitar o JWT da Lambda**: alinhar `TokenValidationParameters` (hoje `ValidateIssuer=false`,
  `ValidateAudience=false`) — passar a validar issuer/audience e usar o mesmo segredo vindo do Secrets Manager.
- **Logs estruturados JSON + correlação**: Serilog (ou `AddJsonConsole`) + middleware de
  `X-Correlation-Id`/`traceparent`, enriquecendo todo log com o id.
- **Instrumentação**: OpenTelemetry (traces + métricas) exportando para New Relic/Datadog, ou o
  agente nativo do vendor. Traces ponta a ponta: API Gateway → Lambda → app → RDS.
- **Métricas de negócio** para os dashboards exigidos: contador de OS criadas, histograma de
  tempo por status (Diagnóstico/Execução/Finalização), contador de falhas de integração.
- **Health checks reais**: `AddNpgSql()` no `/healthz`, separar `/healthz/live` e `/healthz/ready`.
- **OpenAPI fora de Development** (hoje o Scalar só sobe em Development) para poder linkar o Swagger.
- Ajustes de connection string para RDS (SSL obrigatório) e migrations rodando em Job/initContainer
  em vez de no startup, se preferir (opcional).

**C. Kubernetes**
- Remover `k8s/database/` (Postgres em pod) — passa a apontar para o RDS.
- Ingress + annotations do AWS Load Balancer Controller; TLS.
- `resources`, HPA (já existe), PodDisruptionBudget, `topologySpreadConstraints` (opcional).
- Secrets vindo do Secrets Manager (External Secrets Operator ou CSI driver) em vez de
  `k8s/base/02-secret.yaml` commitado.

**D. Terraform**
- Reescrever `infra/` de kind → AWS, dividido nos 2 repos de infra.
- **Backend S3 remoto** e **parar de commitar `terraform.tfstate`** (hoje `infra/terraform.tfstate`
  está versionado — remover e adicionar ao `.gitignore`).
- Workspaces/diretórios para homologação e produção.

**E. CI/CD (GitHub Actions, um conjunto por repo)**
- PR → lint/validate/plan/test (obrigatório pelo branch protection).
- Push em `homolog` → deploy automático no ambiente de homologação.
- Push/merge em `main` → deploy automático em produção.
- App: build → test → imagem no ECR → `kubectl apply`/Helm no EKS → smoke test.
- Infra: `terraform fmt/validate/plan` no PR, `apply` no merge.
- Lambda: build → publish → `aws lambda update-function-code` (ou Terraform/SAM).

**F. Documentação (dentro dos repos)**
- Diagrama de **componentes com visão de nuvem** (API Gateway, Lambda, EKS, RDS, monitoramento) —
  atualizar `docs/arquitetura/diagramas/componentes.md` e `infraestrutura.md`.
- **Diagramas de sequência**: autenticação por CPF e abertura de OS (novos arquivos).
- **RFCs** (`docs/arquitetura/rfcs/`): escolha da nuvem (AWS), escolha do banco (RDS PostgreSQL),
  estratégia de autenticação (Lambda + JWT no API Gateway), escolha da ferramenta de observabilidade.
- **ADRs** (`docs/arquitetura/adrs/`): padrão de comunicação entre módulos, uso de HPA + Cluster
  Autoscaler, banco único com schema por BC, gestão de segredos, estratégia de branching.
- **Justificativa formal do banco + modelo relacional revisado + diagrama ER** — evoluir
  `database-schema.md` (hoje é DDL + regra de FK) para incluir ER em Mermaid e explicação dos
  relacionamentos e ajustes (índices, constraints, performance).
- **README por repositório** com os 7 itens exigidos (propósito, tecnologias, pré-requisitos,
  execução, deploy, explicação da pipeline, diagrama do componente, link Swagger/Postman).

### 3.3 🌐 O que é feito em outro site/software

| Onde | O quê |
|---|---|
| **GitHub (web/CLI)** | Criar os 4 repositórios; em **cada um**: proteger `main` (sem push direto, PR obrigatório, status check), criar branch `homolog` (e protegê-la), adicionar **`soat-architecture`** como colaborador, configurar **Secrets/Variables** (credenciais AWS ou role OIDC, conta ECR/Docker Hub, chave New Relic/Datadog), habilitar Environments (`homolog`/`prod`) se quiser aprovação manual em produção |
| **New Relic ou Datadog** | Criar conta/licença; instalar o agente no cluster (Helm — o manifesto/values mora no repo, mas conta e chave são no site); montar os **dashboards** (volume diário de OS, tempo médio por status, erros de integração, latência, CPU/memória, uptime); configurar **alertas** (falha no processamento de OS, healthcheck down, latência alta) e canais de notificação. **Recomendação: New Relic** — tier gratuito permanente com 100 GB/mês, suficiente e sem risco de expirar antes da entrega, ao contrário do trial de 14 dias do Datadog |
| **AWS Console** | Bootstrap manual descrito em 3.1 (bucket de state, OIDC/IAM, quotas) e verificação/monitoria durante a gravação do vídeo |
| **Docker Hub** (se não migrar para ECR) | Manter/renovar token do registry |
| **YouTube (ou Vimeo)** | Publicar o vídeo ≤ 15 min (não listado), roteirizado nos 6 pontos exigidos — inclusive **dashboard com análise ao vivo** e **logs/traces em execução** |
| **Portal do Aluno (FIAP)** | Submeter o **PDF único** com links dos 4 repos, vídeo, documentações e confirmação do `soat-architecture` |
| **Miro / diagramas** (opcional) | Se preferir desenhar fora do Mermaid — o repo hoje usa Mermaid, que renderiza no GitHub e é mais barato de manter |

---

## 4. Pontos de atenção / riscos

1. **Conta AWS Academy** — se for o caso, restringe IAM, EKS pode não estar liberado e a sessão
   expira. Decidir isso primeiro: define se dá para usar EKS ou se cai para outra estratégia.
   **É a decisão bloqueante do projeto todo.**
2. **Custo** — EKS (~US$0,10/h de control plane) + NAT Gateway + RDS + ALB rodando 24/7 pesa.
   Estratégia: subir para gravar o vídeo e destruir depois, mantendo o histórico do Actions como
   evidência do "deploy ativo".
3. **`terraform.tfstate` commitado** no repo atual — remover do versionamento junto com o split.
4. **Segredo JWT compartilhado** entre Lambda e app é o ponto frágil da integração; resolver via
   Secrets Manager desde o começo (ou usar chave assimétrica RS256 + JWKS, mais limpo para o
   JWT authorizer do API Gateway).
5. **Métricas de negócio dos dashboards** são desenvolvimento na aplicação, não configuração —
   costumam ser subestimadas no planejamento.
6. **Dependência entre repos** — a ordem de apply (db → k8s → lambda/app) precisa estar documentada
   e refletida nos pipelines, senão o deploy automático quebra.
