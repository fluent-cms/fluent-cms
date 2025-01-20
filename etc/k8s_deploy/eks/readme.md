# Deploy Fluent CMS to eks
This guid provides instructions of setting up Fluent CMS on eks k8s cluster.
## Create Cluster
cd `cluster`, run
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
## Create ebs
cd `ebs`, run
```shell
terraform init
terraform apply --auto-approve
```
check the ebs driver is installed
```shell
kubectl get pods -n kube-system -l app=ebs-csi-controller
```
## Create DB and app
cd `db-app`
add a file `terraform.tfvars` , input the db_password and the kube_context
```yaml
db_password = "3saxYX3kbx"
kube_context = "arn:aws:eks:us-east-1:773594168572:cluster/cms-aws"
```
```shell
terraform init
terraform apply --auto-approve
```

## Verify Server
```shell
kubectl get service
kubectl port-forward svc/cms-app-aspnet-core 8080:80
```
then use the browser to navigate to http://localhost:8080 


