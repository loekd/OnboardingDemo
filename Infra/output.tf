output "spn_client_id" {
  sensitive = false
  description = "Client ID of the service principal"
  value       = azuread_service_principal.onboarding_admin_spn.application_id
}

output "spn_client_secret" {
  sensitive = true
  description = "Client Secret of the service principal"
  value       = azuread_service_principal_password.onboarding_admin_spn_password.value
}

output "spn_subscription_id" {
  sensitive = false
  description = "Subscription of the databases"
  value       = data.azurerm_subscription.current.subscription_id
}

output "spn_tenant_id" {
  sensitive = false
  description = "Tenant of the subscription"
  value       = data.azurerm_subscription.current.tenant_id
}


output "impersonation_identity_client_id" {
  sensitive = false
  description = "Client ID of the managed identity used for impersonation when calling onboarding API from screening API"
  value       = azurerm_user_assigned_identity.external_screening_identity.client_id
}

output "impersonation_identity_object_id" {
  sensitive = false
  description = "Object ID of the managed identity used for impersonation when calling onboarding API from screening API"
  value       = azurerm_user_assigned_identity.external_screening_identity.principal_id
}