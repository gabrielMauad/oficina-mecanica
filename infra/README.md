# Infraestrutura como Código (Terraform) — Oficina Mecânica

Este módulo Terraform provisiona, de forma declarativa, um cluster Kubernetes local (kind),
o metrics-server (pré-requisito do HPA) e todos os manifestos da aplicação (`/k8s`): namespace,
ConfigMap, Secret, banco PostgreSQL (PVC + Deployment + Service) e a API (Deployment + Service +
HPA).

Tudo é criado via *resources* Terraform de verdade (`kind_cluster`, `helm_release`,
`kubectl_manifest`) — nenhum `local-exec` chamando `kubectl` por fora.

## Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/)
- [Terraform](https://developer.hashicorp.com/terraform/downloads) ≥ 1.5
- [kind](https://kind.sigs.k8s.io/) no PATH
- [kubectl](https://kubernetes.io/docs/tasks/tools/) no PATH

## Recursos criados

| Recurso Terraform | Tipo | O que cria |
|---|---|---|
| `kind_cluster.this` | `kind_cluster` | Cluster Kubernetes local (kind) com NodePort 30080 mapeado |
| `helm_release.metrics_server` | `helm_release` | metrics-server em `kube-system` (habilita o HPA) |
| `kubectl_manifest.namespace` | `kubectl_manifest` | Namespace `oficina-mecanica` |
| `kubectl_manifest.configmap` | `kubectl_manifest` | ConfigMap com variáveis não sensíveis |
| `kubectl_manifest.secret` | `kubectl_manifest` | Secret com credenciais do banco/JWT |
| `kubectl_manifest.postgres_pvc` | `kubectl_manifest` | PersistentVolumeClaim do PostgreSQL |
| `kubectl_manifest.postgres_deployment` | `kubectl_manifest` | Deployment do PostgreSQL |
| `kubectl_manifest.postgres_service` | `kubectl_manifest` | Service do PostgreSQL |
| `kubectl_manifest.api_deployment` | `kubectl_manifest` | Deployment da API (imagem parametrizada via `var.api_image`) |
| `kubectl_manifest.api_service` | `kubectl_manifest` | Service (NodePort 30080) da API |
| `kubectl_manifest.api_hpa` | `kubectl_manifest` | HorizontalPodAutoscaler da API |

## Como a imagem chega no cluster

- **Local:** o cluster kind não enxerga imagens do Docker local automaticamente. Depois de criar
  o cluster, carregue a imagem com `kind load docker-image ... --name oficina-mecanica`. Como o
  `imagePullPolicy` é `IfNotPresent`, o pod usa a imagem carregada sem tentar puxar de um
  registry.
- **CI:** a imagem é publicada no Docker Hub (repositório público) e `var.api_image` aponta para
  lá; o pod puxa do Docker Hub. Não precisa de `kind load` nem de `imagePullSecret`.

## Como aplicar — LOCAL (passo a passo)

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

## Como aplicar — CI (resumo)

```bash
cd infra
terraform init
terraform apply -auto-approve -var="api_image=docker.io/<user>/oficina-mecanica-api:<tag>"
```

Aqui não há `-target` nem `kind load`: a imagem vem do Docker Hub, então um único apply cria o
cluster e sobe tudo.

## Como acessar

- API: `http://localhost:30080/healthz`, `http://localhost:30080/scalar`
- Alternativa: `kubectl port-forward -n oficina-mecanica svc/oficina-api 8080:8080`

## Como destruir

```bash
cd infra
terraform destroy -auto-approve      # remove o cluster kind e tudo dentro dele
```

## Troubleshooting

- **Erro de provider na 1ª vez (kubeconfig inexistente):** se `terraform apply` falhar porque o
  kubeconfig ainda não existe, rode primeiro `terraform apply -target=kind_cluster.this` e depois
  `terraform apply` (é o fluxo local de 2 passos, também resolve isto).
- **Pod da API em `ImagePullBackOff` (local):** você esqueceu o
  `kind load docker-image ... --name oficina-mecanica`.
- **Pod da API em `CrashLoopBackOff`:** olhe `kubectl logs -n oficina-mecanica deploy/oficina-api`;
  normalmente é o banco ainda não pronto (o initContainer deve evitar) ou connection string
  divergente da senha do Postgres na Secret.
- **HPA `<unknown>`:** metrics-server sem `--kubelet-insecure-tls` ou ainda coletando as
  primeiras métricas (aguarde ~30s).
