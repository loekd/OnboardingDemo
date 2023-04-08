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

variable "acr_client_ip" {
  description = "Client allowed to access ACR"
  type        = string
}

variable "onboarding_app_version" {
  description = "Onboarding app version"
  type        = string
  default     = "0.1"
}

variable "identity_server_app_version" {
  description = "External screening identity server app version"
  type        = string
  default     = "0.1"
}

variable "screening_api_app_version" {
  description = "External screening api app version"
  type        = string
  default     = "0.1"
}
