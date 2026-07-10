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
