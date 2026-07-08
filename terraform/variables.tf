variable "project_name" {
  type        = string
  description = "Project name used for Azure resource names."
  default     = "hotelia"
}

variable "environment" {
  type        = string
  description = "Deployment environment."
  default     = "dev"
}

variable "azure_region" {
  type        = string
  description = "Azure region for all Hotelia resources."
  default     = "brazilsouth"
}

variable "key_vault_name" {
  type        = string
  description = "Optional custom Key Vault name. Must be globally unique in Azure."
  default     = null
}