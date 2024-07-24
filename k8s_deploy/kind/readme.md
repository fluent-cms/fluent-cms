# Deploy Fluent CMS on Kind
This guide provides instructions for setting up and testing a Kubernetes cluster using Kind, Helm, Terraform, and kubectl.
Test everything on Kind first to save the cost of cloud Kubernetes services like EKS.
## Prerequisites
- kind: kind is a tool for running local Kubernetes clusters using Docker container “nodes”.
- helm: Helm is the package manager for Kubernetes
- terraform: Terraform is an infrastructure as code tool that lets you build, change, and version infrastructure safely and efficiently.
- kubectl: Kubernetes provides a command line tool for communicating with a Kubernetes cluster's control plane, using the Kubernetes API.
## install
1. create a cluster
   ```shell
   kind create cluster --name cms     
   ```
   then check if kind-cms is the active context
   ```shell
   kubectl config get-contexts
   CURRENT   NAME                                                 CLUSTER                                              AUTHINFO                                             NAMESPACE
   *         kind-cms                                             kind-cms                                             kind-cms   
   ```
2. create resource
   cd `fluent-cms/k8_deploy/kind`
   ```shell
   terraform apply
   ```
3. test installation
   ```shell
   kubectl get service   #check if cms-app-aspnet-core exists
   kubectl port-forward svc/cms-app-aspnet-core 8080:80 &
   ```
   then use a browser navigate to `http://localhost:8080`, see if everything works
4. check the app log
   ```shell
   kubectl get pod  # find the pod whose name starts with cms-app
   kubectl logs cms-app-aspnet-core-6976f7b57f-rttwp -f
   ```
## debug helm config individually
if there are something wrong, we can use helm to debug.
### postgres-helm
1. install
    ```shell
    helm install cms-db bitnami/postgresql --version 15.5.17 -f postgres-helm/values.yaml
    ```
2. test installation
    ```shell
    kubectl get service   #check if cms-db-postgresql exists
    kubectl run psql-postgresql-client --rm --tty -i --restart='Never' --image docker.io/bitnami/postgresql:16.3.0-debian-12-r19 \
      --env='PGPASSWORD=${DB_PASSWORD}' \
      --command -- psql --host cms-db-postgresql -U postgres -d postgres -p 5432
    ```
3. uninstall
    ```shell
    helm uninstall  psql
    ```
4. need manually delete pv and pvc after after uninstall helm
   ```
   kubectl delete pvc --all && kubectl delete pv --all
   ```
### dotnet-helm
1. install
   ```shell
   helm install cms-app bitnami/aspnet-core --version 3.5.5 -f dotnet-helm/values.yaml
   ```
2. test installation
   ```shell
   kubectl get service   #check if cms-app-aspnet-core exists
   kubectl port-forward svc/cms-app-aspnet-core 8080:80 &
   ```
3. check the app log
   ```shell
   kubectl get pod  # find the pod whose name starts with cms-app
   kubectl logs cms-app-aspnet-core-6976f7b57f-rttwp -f
   ```
4. uninstall
   ```shell
   helm uninstall cms-app
   ```
