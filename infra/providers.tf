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
