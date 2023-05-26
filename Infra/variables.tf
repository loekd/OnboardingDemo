variable "location" {
  description = "Location for the resources"
  type        = string
  default     = "westeurope"
}

variable "acr_client_ip" {
  description = "Client allowed to access ACR"
  type        = string
}

variable "onboarding_app_version" {
  description = "Onboarding app version"
  type        = string
  default     = "0.39"
}

variable "identity_server_app_version" {
  description = "External screening identity server app version"
  type        = string
  default     = "0.39"
}

variable "screening_api_app_version" {
  description = "External screening api app version"
  type        = string
  default     = "0.39"
}

variable "screening_client_secret" {
  description = "Client secret for screening api"
  type        = string
}

variable "min_replicas" {
  description = "Minimum number of replicas"
  type        = number
  default     = 0
}

variable "max_replicas" {
  description = "Maximum number of replicas"
  type        = number
  default     = 1
}
