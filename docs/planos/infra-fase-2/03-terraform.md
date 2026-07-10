# 03 — Terraform (IaC em `/infra`)

> Pré-requisito de leitura: `00-visao-geral.md` e `02-kubernetes.md`. Conceitos em
> `docs/aulas/consolidado_01_terraform.md`. Padrão de aplicar manifestos via Terraform validado
> pelo professor em `docs/perguntas-infra.md`.

Objetivo: provisionar, de forma declarativa, (a) o **cluster kind**, (b) o **metrics-server**
(pré-requisito do HPA) e (c) **todos os manifestos** do `/k8s` — usando **resources de verdade**
(`kind_cluster`, `helm_release`, `kubectl_manifest`), **nunca** `local-exec` chamando `kubectl`
(regra do professor).

Divisão adotada (aceita pelo fórum): o Terraform provisiona **cluster + base + banco + app**,
entregando um sistema completo em `terraform apply`. O CI/CD (`04`) reusa este mesmo Terraform
para fazer o deploy no runner.

## Providers usados

| Provider | Source | Papel |
|---|---|---|
| kind | `tehcyx/kind` `~> 0.4` | cria/destrói o cluster kind (`kind_cluster`) |
| kubectl | `gavinbunney/kubectl` `~> 1.14` | aplica manifestos YAML (`kubectl_manifest`) |
| helm | `hashicorp/helm` `~> 2.12` | instala o metrics-server (`helm_release`) |

Pré-requisitos na máquina/runner: **Docker**, **Terraform ≥ 1.5**, **kind** e **kubectl** no PATH.

---

## `infra/versions.tf`

```hcl
terraform {
  required_version = ">= 1.5.0"

  required_providers {
    kind = {
      source  = "tehcyx/kind"
      version = "~> 0.4"
    }
    kubectl = {
      source  = "gavinbunney/kubectl"
      version = "~> 1.14"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.12"
    }
  }
}
```

## `infra/variables.tf`

```hcl
variable "cluster_name" {
  description = "Nome do cluster kind"
  type        = string
  default     = "oficina-mecanica"
}

variable "node_image" {
  description = "Imagem do nó kind (versão do Kubernetes)"
  type        = string
  default     = "kindest/node:v1.31.0"
}

variable "api_image" {
  description = "Imagem da API a implantar. Local: oficina-mecanica-api:local (via kind load). CI: docker.io/<user>/oficina-mecanica-api:<tag>"
  type        = string
  default     = "oficina-mecanica-api:local"
}

variable "manifests_path" {
  description = "Caminho para a pasta k8s (relativo ao módulo infra/)"
  type        = string
  default     = "../k8s"
}
```

## `infra/providers.tf`

O cluster kind escreve um kubeconfig em um caminho conhecido; os providers kubectl e helm leem
esse arquivo. Usar um caminho fixo (local) evita ambiguidade de codificação de certificados.

```hcl
locals {
  kubeconfig_path = pathexpand("${path.module}/kubeconfig.yaml")
}

provider "kind" {}

provider "kubectl" {
  config_path      = local.kubeconfig_path
  load_config_file = true
}

provider "helm" {
  kubernetes {
    config_path = local.kubeconfig_path
  }
}
```

## `infra/main.tf`

```hcl
# ---------------------------------------------------------------------------
# 1. Cluster kind
# ---------------------------------------------------------------------------
resource "kind_cluster" "this" {
  name            = var.cluster_name
  node_image      = var.node_image
  kubeconfig_path = local.kubeconfig_path
  wait_for_ready  = true

  kind_config {
    kind        = "Cluster"
    api_version = "kind.x-k8s.io/v1alpha4"

    node {
      role = "control-plane"

      # expõe o NodePort 30080 do Service da API em localhost:30080
      extra_port_mappings {
        container_port = 30080
        host_port      = 30080
      }
    }
  }
}

# ---------------------------------------------------------------------------
# 2. metrics-server (pré-requisito do HPA) — com a flag necessária no kind
# ---------------------------------------------------------------------------
resource "helm_release" "metrics_server" {
  name       = "metrics-server"
  repository = "https://kubernetes-sigs.github.io/metrics-server/"
  chart      = "metrics-server"
  namespace  = "kube-system"

  # essencial no kind: sem isto o metrics-server não coleta métricas e o HPA fica <unknown>
  set {
    name  = "args[0]"
    value = "--kubelet-insecure-tls"
  }

  depends_on = [kind_cluster.this]
}

# ---------------------------------------------------------------------------
# 3. Base: namespace, ConfigMap, Secret
# ---------------------------------------------------------------------------
resource "kubectl_manifest" "namespace" {
  yaml_body  = file("${var.manifests_path}/base/00-namespace.yaml")
  depends_on = [kind_cluster.this]
}

resource "kubectl_manifest" "configmap" {
  yaml_body  = file("${var.manifests_path}/base/01-configmap.yaml")
  depends_on = [kubectl_manifest.namespace]
}

resource "kubectl_manifest" "secret" {
  yaml_body  = file("${var.manifests_path}/base/02-secret.yaml")
  depends_on = [kubectl_manifest.namespace]
}

# ---------------------------------------------------------------------------
# 4. Banco de dados (PVC, Deployment, Service)
# ---------------------------------------------------------------------------
resource "kubectl_manifest" "postgres_pvc" {
  yaml_body  = file("${var.manifests_path}/database/10-postgres-pvc.yaml")
  depends_on = [kubectl_manifest.namespace]
}

resource "kubectl_manifest" "postgres_deployment" {
  yaml_body  = file("${var.manifests_path}/database/11-postgres-deployment.yaml")
  depends_on = [kubectl_manifest.postgres_pvc, kubectl_manifest.secret]
}

resource "kubectl_manifest" "postgres_service" {
  yaml_body  = file("${var.manifests_path}/database/12-postgres-service.yaml")
  depends_on = [kubectl_manifest.postgres_deployment]
}

# ---------------------------------------------------------------------------
# 5. Aplicação (Deployment com imagem parametrizada, Service, HPA)
# ---------------------------------------------------------------------------
resource "kubectl_manifest" "api_deployment" {
  # injeta a imagem no lugar do placeholder do manifesto
  yaml_body = replace(
    file("${var.manifests_path}/app/20-api-deployment.yaml"),
    "IMAGE_PLACEHOLDER",
    var.api_image
  )
  depends_on = [
    kubectl_manifest.configmap,
    kubectl_manifest.secret,
    kubectl_manifest.postgres_service,
  ]
}

resource "kubectl_manifest" "api_service" {
  yaml_body  = file("${var.manifests_path}/app/21-api-service.yaml")
  depends_on = [kubectl_manifest.api_deployment]
}

resource "kubectl_manifest" "api_hpa" {
  yaml_body  = file("${var.manifests_path}/app/22-api-hpa.yaml")
  depends_on = [kubectl_manifest.api_deployment, helm_release.metrics_server]
}
```

## `infra/outputs.tf`

```hcl
output "cluster_name" {
  description = "Nome do cluster kind criado"
  value       = kind_cluster.this.name
}

output "kubeconfig_path" {
  description = "Caminho do kubeconfig gerado para este cluster"
  value       = local.kubeconfig_path
}

output "api_url_nodeport" {
  description = "URL da API via NodePort mapeado pelo kind"
  value       = "http://localhost:30080"
}

output "api_image" {
  description = "Imagem da API implantada"
  value       = var.api_image
}
```

## `infra/terraform.tfvars.example`

```hcl
# copie para terraform.tfvars e ajuste conforme necessário
cluster_name = "oficina-mecanica"
# local (após 'kind load'):   oficina-mecanica-api:local
# CI (Docker Hub):            docker.io/SEU_USUARIO/oficina-mecanica-api:latest
api_image    = "oficina-mecanica-api:local"
```

Adicione ao `.gitignore` da raiz (se ainda não houver): `infra/.terraform/`,
`infra/kubeconfig.yaml`, `infra/terraform.tfvars`, `infra/*.tfstate*`.

---

## Como a imagem chega no cluster

- **Local:** o cluster kind não enxerga imagens do Docker local automaticamente. Depois de
  criar o cluster, carregue a imagem com `kind load docker-image ... --name oficina-mecanica`.
  Como o `imagePullPolicy` é `IfNotPresent`, o pod usa a imagem carregada sem tentar puxar de
  um registry.
- **CI (04):** a imagem é publicada no Docker Hub (repositório público) e o `api_image` aponta
  para lá; o pod puxa do Docker Hub. Não precisa de `kind load` nem de `imagePullSecret`.

## Fluxo de aplicação — LOCAL (passo a passo)

Por causa do `kind load` (que precisa do cluster já existente), o primeiro apply local é feito
em duas partes:

```bash
cd infra
terraform init

# 1. cria só o cluster kind primeiro
terraform apply -auto-approve -target=kind_cluster.this

# 2. builda a imagem (a partir da RAIZ do repo) e carrega no cluster
cd ..
docker build -f src/Bootstrap/Api/Dockerfile -t oficina-mecanica-api:local .
kind load docker-image oficina-mecanica-api:local --name oficina-mecanica

# 3. aplica o resto (metrics-server, base, banco, app, hpa)
cd infra
terraform apply -auto-approve
```

Nos applies seguintes (cluster já existe), um único `terraform apply` basta — só refaça o
`docker build` + `kind load` quando o código da aplicação mudar.

## Fluxo de aplicação — CI (resumo; detalhe em `04`)

```bash
cd infra
terraform init
terraform apply -auto-approve -var="api_image=docker.io/<user>/oficina-mecanica-api:<tag>"
```

Aqui não há `-target` nem `kind load`: a imagem vem do Docker Hub, então um único apply cria o
cluster e sobe tudo.

## Destruir

```bash
cd infra
terraform destroy -auto-approve      # remove o cluster kind e tudo dentro dele
```

---

## `infra/README.md` (ENTREGÁVEL — documentar recursos e como aplicar)

O requisito de IaC exige **"documentar quais recursos estão sendo criados e como aplicar"**.
Crie `infra/README.md` com, no mínimo:

1. **Pré-requisitos** (Docker, Terraform ≥ 1.5, kind, kubectl).
2. **Tabela de recursos criados**, por exemplo:

   | Recurso Terraform | Tipo | O que cria |
   |---|---|---|
   | `kind_cluster.this` | `kind_cluster` | Cluster Kubernetes local (kind) com NodePort 30080 mapeado |
   | `helm_release.metrics_server` | `helm_release` | metrics-server em kube-system (habilita o HPA) |
   | `kubectl_manifest.namespace` | `kubectl_manifest` | Namespace `oficina-mecanica` |
   | `kubectl_manifest.configmap` | `kubectl_manifest` | ConfigMap com variáveis não sensíveis |
   | `kubectl_manifest.secret` | `kubectl_manifest` | Secret com credenciais do banco/JWT |
   | `kubectl_manifest.postgres_*` | `kubectl_manifest` | PVC + Deployment + Service do PostgreSQL |
   | `kubectl_manifest.api_*` | `kubectl_manifest` | Deployment + Service (NodePort) + HPA da API |

3. **Como aplicar** (os fluxos local e CI acima).
4. **Como acessar** (`http://localhost:30080/healthz` e `/scalar`, ou `kubectl port-forward`).
5. **Como destruir** (`terraform destroy`).
6. **Troubleshooting** (ver abaixo).

## Como validar (Definition of Done desta etapa)

```bash
cd infra && terraform apply -auto-approve        # (após o fluxo local de 2 passos na 1a vez)

kubectl get pods -n oficina-mecanica             # oficina-postgres e oficina-api Running
kubectl get hpa -n oficina-mecanica              # TARGETS deve mostrar % (não <unknown>)
kubectl top pods -n oficina-mecanica             # deve retornar CPU/memória (metrics-server OK)
curl -i http://localhost:30080/healthz           # 200
```

Se `TARGETS` ficar `<unknown>`, o metrics-server não subiu com a flag — confira o
`helm_release.metrics_server` e rode `kubectl -n kube-system logs deploy/metrics-server`.

## Troubleshooting (colocar no infra/README.md)

- **Erro de provider na 1ª vez (kubeconfig inexistente):** se `terraform apply` falhar porque
  o kubeconfig ainda não existe, rode primeiro `terraform apply -target=kind_cluster.this` e
  depois `terraform apply` (é o fluxo local de 2 passos, também resolve isto).
- **Pod da API em `ImagePullBackOff` (local):** você esqueceu o `kind load docker-image ... --name oficina-mecanica`.
- **Pod da API em `CrashLoopBackOff`:** olhe `kubectl logs -n oficina-mecanica deploy/oficina-api`;
  normalmente é o banco ainda não pronto (o initContainer deve evitar) ou connection string
  divergente da senha do Postgres na Secret.
- **HPA `<unknown>`:** metrics-server sem `--kubelet-insecure-tls` ou ainda coletando as
  primeiras métricas (aguarde ~30s).
