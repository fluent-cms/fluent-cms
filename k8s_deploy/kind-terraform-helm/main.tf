provider "kubernetes" {
  config_path    = var.kube_config_path
  config_context = var.kube_context
}

provider "helm" {
  kubernetes {
    config_path    = var.kube_config_path
    config_context = var.kube_context
  }
}

resource "helm_release" "cmsDb" {
  name       = "cms-db"
  repository = "https://charts.bitnami.com/bitnami"
  chart      = "postgresql"
  version    = "15.5.17"
  values     = [templatefile("postgres-helm/values.yaml", { DB_PASSWORD = var.db_password })]
}

resource "helm_release" "cmsApp" {
  name       = "cms-app"
  repository = "https://charts.bitnami.com/bitnami"
  chart      = "aspnet-core"
  version    = "6.2.7"
  values     = [templatefile("dotnet-helm/values.yaml", { DB_PASSWORD  = var.db_password })]
  depends_on = [
    helm_release.cmsDb
  ]
}