provider "kubernetes" {
  config_path    = "~/.kube/config"
  config_context = "kind-kind"
}

provider "helm" {
  kubernetes {
    config_path    = "~/.kube/config"
    config_context = "kind-kind"
  }
}

resource "helm_release" "cmsDb" {
  name       = "cms-db"
  repository = "https://charts.bitnami.com/bitnami"
  chart      = "postgresql"
  version    = "15.5.17"
  values     = [
    <<-EOF
auth:
  password: 3saxYX3kbx
  existingSecret: ""
primary:
  initdb:
    scripts:
      init.sql: |
        CREATE DATABASE cms;
    EOF
  ]
}

resource "helm_release" "cmsApp" {
  name       = "cms-app"
  repository = "https://charts.bitnami.com/bitnami"
  chart      = "aspnet-core"
  version    = "6.2.7"
  values     = [
    <<-EOF
image:
  registry: docker.io
  repository: jaike/fluent-cms
  tag: "0.3"
appFromExternalRepo:
  enabled: false
command: ["dotnet"]
args: ["FluentCMS.dll"]
extraEnvVars:
  - name: ASPNETCORE_ENVIRONMENT
    value: Production
  - name: DatabaseProvider
    value: Postgres
  - name: Postgres
    value: Host=cms-db-postgresql;Database=cms;Username=postgres;Password=3saxYX3kbx
    EOF
  ]
  depends_on = [
    helm_release.cmsDb
  ]
}
