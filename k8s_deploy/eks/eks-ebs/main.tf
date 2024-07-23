data "aws_partition" "current_testing" {}

data "aws_caller_identity" "current_testing" {}

data "aws_eks_cluster" "cluster_testing" {
  name = var.cluster_name
}

locals {
  partition          = data.aws_partition.current_testing.id
  account_id         = data.aws_caller_identity.current_testing.account_id
  oidc_provider_arn  = replace(data.aws_eks_cluster.cluster_testing.identity[0].oidc[0].issuer, "https://", "")
  oidc_provider_name = "arn:${local.partition}:iam::${local.account_id}:oidc-provider/${local.oidc_provider_arn}"
}

###################
# EBS CSI Role    #
###################

module "ebs_csi_eks_role" {
  source    = "terraform-aws-modules/iam/aws//modules/iam-role-for-service-accounts-eks"
  role_name = "ebs_csi"

  attach_ebs_csi_policy = true

  oidc_providers = {
    main = {
      provider_arn               = local.oidc_provider_name
      namespace_service_accounts = ["kube-system:ebs-csi-controller-sa"]
    }
  }
}


###################
# EBS CSI Driver  #
###################

resource "helm_release" "ebs_csi_driver" {
  name       = "aws-ebs-csi-driver"
  namespace  = "kube-system"
  repository = "https://kubernetes-sigs.github.io/aws-ebs-csi-driver"
  chart      = "aws-ebs-csi-driver"

  set {
    name  = "controller.serviceAccount.annotations.eks\\.amazonaws\\.com/role-arn"
    type  = "string"
    value = module.ebs_csi_eks_role.iam_role_arn
  }
}


###################
# Storage Classes #
###################

resource "kubernetes_storage_class_v1" "storageclass_gp2" {
  depends_on = [helm_release.ebs_csi_driver, module.ebs_csi_eks_role]
  metadata {
    name = "gp2-encrypted"
    annotations = {
      "storageclass.kubernetes.io/is-default-class" = "true"
    }
  }

  storage_provisioner    = "ebs.csi.aws.com"
  reclaim_policy         = "Delete"
  allow_volume_expansion = true
  volume_binding_mode    = "WaitForFirstConsumer"

  parameters = {
    type      = "gp2"
    encrypted = "true"
  }

}