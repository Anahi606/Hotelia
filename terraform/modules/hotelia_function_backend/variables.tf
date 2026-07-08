variable "name_prefix" {
  type        = string
  description = "Name prefix for Azure resources."
}

variable "project_name" {
  type        = string
  description = "Project name used for Azure resource names."
}

variable "environment" {
  type        = string
  description = "Deployment environment."
}

variable "location" {
  type        = string
  description = "Azure region for module resources."
}

variable "resource_group_name" {
  type        = string
  description = "Resource group where module resources will be created."
}

variable "key_vault_name" {
  type        = string
  description = "Optional custom Key Vault name. Must be globally unique in Azure."
  default     = null
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to Azure resources."
  default     = {}
}