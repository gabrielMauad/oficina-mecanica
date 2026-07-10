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
