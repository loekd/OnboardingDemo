variable "location" {
  description = "Location for the resources"
  type        = string
  default     = "westeurope"
}

variable "object_id_key_vault_accesspolicy" {
  description = "Azure Workstream group in the backbase tenant"
  type        = string
  default     = "31b320c0-5e2b-4d9b-9602-0da88f519a6e"
}
