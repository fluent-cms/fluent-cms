variable "db_password" {
  description = "Password for the PostgreSQL database"
  type        = string
  sensitive   = true
}

variable "kube_config_path" {
  default = "~/.kube/config"
}

variable "kube_context" {
  default = "arn:aws:eks:us-east-1:773594168572:cluster/cms-aws"
}

