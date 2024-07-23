output "ebs_csi_iam_role_arn" {
  description = "IAM role arn of ebs csi"
  value       = module.ebs_csi_eks_role.iam_role_arn
}