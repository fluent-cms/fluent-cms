# Deploy Asp.net Core Application(Fluent‐CMS) to Cloud (EKS ‐ AWS Elastic Kubernetes Service) using terraform and helm
With tools like Terraform and Helm available today, deploying applications to the cloud has become more accessible for 
developers with a background in networking or computer science. 
Developers have the advantage of treating infrastructure as code, which simplifies the deployment process.
## Debug/Test your code/config in local environment
Cloud is not free, and operations on cloud is slow, so test your code and config in local environment save both money and time. I am using Kind, here is the list
### Prerequisites
- kind: kind is a tool for running local Kubernetes clusters using Docker container “nodes”.
- helm: Helm is the package manager for Kubernetes
- terraform: Terraform is an infrastructure as code tool that lets you build, change, and version infrastructure safely and efficiently.
- kubectl: Kubernetes provides a command line tool for communicating with a Kubernetes cluster's control plane, using the Kubernetes API.
### Overview
![deploy-kind-overview.png](diagrams%2Fdeploy-kind-overview.png)
### Install
1. create a cluster
   ```shell
   kind create cluster --name cms     
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
   Then use a browser navigate to `http://localhost:8080`, see if everything works.
4. check the app log
   ```shell
   kubectl get pod  # find the pod whose name starts with cms-app
   kubectl logs cms-app-aspnet-core-6976f7b57f-rttwp -f
   ```
## Deploy to EKS
Real Cloud Environment is a little difference than local test environment,
- We have to install EBS(Elastic Block Store) to create persistence volume.
- We have to install ELB(Elastic Load Balancing) to expose our service to public. 

For simplicity purpose, I will still use port forwarding to test our service.   
![deploy-eks-overview.png](diagrams%2Fdeploy-eks-overview.png)

### Create Cluster
cd `fluent-cms/k8_deploy/cluster`, run
```shell
terraform init
terraform apply --auto-approve
aws eks update-kubeconfig --region us-east-1 --name cms-aws
```
check the context name
```shell
CURRENT   NAME                                                 CLUSTER                                              AUTHINFO                                             NAMESPACE
*         arn:aws:eks:us-east-1:773594168572:cluster/cms-aws   arn:aws:eks:us-east-1:773594168572:cluster/cms-aws   arn:aws:eks:us-east-1:773594168572:cluster/cms-aws
```
### Create ebs
cd `fluent-cms/k8_deploy/ebs`, run
```shell
terraform init
terraform apply --auto-approve
```
check the ebs driver is installed
```shell
kubectl get pods -n kube-system -l app=ebs-csi-controller
```
### Create DB and app
cd `fluent-cms/k8_deploy/db-app`
add a file `terraform.tfvars` , input the db_password and the kube_context
```yaml
db_password = <your password>
kube_context = "arn:aws:eks:us-east-1:773594168572:cluster/cms-aws"
```
```shell
terraform init
terraform apply --auto-approve
```

### Verify Server
```shell
kubectl get service
kubectl port-forward svc/cms-app-aspnet-core 8080:80
```
then use the browser to navigate to http://localhost:8080 

## Customization of Helm Package
### PostgreSQL
The helm package can be find at https://artifacthub.io/packages/helm/bitnami/postgresql .
There are some values we need to customize, as in `fluent-cms/k8_deploy/db-app/postgres-helm`
1. ***postgresPassword***, we provide password from terraForm variable `DB_PASSWORD`, will use same password in app's connection string
2. ***primary.initdb.scripts***, need to create a database
3. persistence.storageClass, need to change it to a class the cloud provider support.

### Fluent CMS App
The helm package can be find at https://artifacthub.io/packages/helm/bitnami/aspnet-core .
There are some values we need to customize, as in `fluent-cms/k8_deploy/db-app/dot-net-helm`
1. ***the docker image***, we need to change to our own repository and tag.
2. ***args***, we change it to our app name(FluentCMS.dll) 
3. DatabaseProvider and Connection string
