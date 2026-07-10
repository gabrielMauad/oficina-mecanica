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
