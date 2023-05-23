locals {
  onboarding_readwrite_role_id  = "f65518d6-1002-426e-b184-bd67b0646d6d"
  onboarding_read_role_id       = "c2f1d4bd-c134-4584-80e3-9aa3ec94d06f"
  onboarding_readwrite_scope_id = "7d651468-1aed-41c4-999f-01e57c3f7388"
  onboarding_read_scope_id      = "8d443e2b-1de4-4b34-9963-2a8db36ea85a"
}
//an azure ad app registration for the onboarding api, exposing 2 api scopes
resource "azuread_application" "onboarding_api" {
  display_name     = "OnboardingApi"
  owners           = [data.azuread_client_config.current.object_id]
  sign_in_audience = "AzureADMyOrg"
  identifier_uris  = ["api://4eeeb950-3fc9-4bd2-9279-51901fa33891"]

  api {
    mapped_claims_enabled          = false
    requested_access_token_version = 1

    oauth2_permission_scope {
      admin_consent_description  = "Onboarding.ReadWrite access for admins"
      admin_consent_display_name = "Onboarding.ReadWrite"
      enabled                    = true
      id                         = local.onboarding_readwrite_scope_id
      type                       = "Admin"
      user_consent_description   = "Onboarding.ReadWrite access for users"
      user_consent_display_name  = "Onboarding.ReadWrite"
      value                      = "Onboarding.ReadWrite"
    }

    oauth2_permission_scope {
      admin_consent_description  = "Onboarding.Read access for admins"
      admin_consent_display_name = "Onboarding.Read"
      enabled                    = true
      id                         = local.onboarding_read_scope_id
      type                       = "Admin"
      user_consent_description   = "Onboarding.Read access for users"
      user_consent_display_name  = "Onboarding.Read"
      value                      = "Onboarding.Read"
    }
  }

  app_role {
    allowed_member_types = ["Application"]
    description          = "ability to read and write api data"
    display_name         = "Onboarding.ReadWriteRole"
    enabled              = true
    id                   = local.onboarding_readwrite_role_id
    value                = "Onboarding.ReadWriteRole"
  }

  app_role {
    allowed_member_types = ["Application"]
    description          = "Ability to read api data"
    display_name         = "Onboarding.ReadRole"
    enabled              = true
    id                   = local.onboarding_read_role_id
    value                = "Onboarding.ReadRole"
  }
}

resource "azuread_service_principal" "onboarding_api" {
  application_id = azuread_application.onboarding_api.application_id
}

//The frontend app registration, exposing 2 api scopes and 2 roles
resource "azuread_application" "onboarding_frontend" {
  display_name     = "OnboardingFrontEnd"
  owners           = [data.azuread_client_config.current.object_id]
  sign_in_audience = "AzureADMyOrg"

  single_page_application {
    redirect_uris = ["https://localhost/authentication/login-callback", "https://${azurerm_container_app.onboarding_app.ingress.0.fqdn}/authentication/login-callback"]
  }

  required_resource_access {
    resource_app_id = data.azuread_application_published_app_ids.well_known.result.MicrosoftGraph

    resource_access {
      id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d" # User.Read
      type = "Scope"
    }
  }

  required_resource_access {
    resource_app_id = azuread_application.onboarding_api.application_id

    resource_access {
      id   = local.onboarding_readwrite_scope_id
      type = "Scope"
    }

    resource_access {
      id   = local.onboarding_read_scope_id
      type = "Scope"
    }

    resource_access {
      id   = local.onboarding_readwrite_role_id
      type = "Role"
    }

    resource_access {
      id   = local.onboarding_read_role_id
      type = "Role"
    }
  }
}

//user assigned managed identity to impersonate when calling onboarding api from external screening api
resource "azurerm_user_assigned_identity" "external_screening_identity" {
  name                = "external-screening-identity"
  resource_group_name = azurerm_resource_group.rg_onboarding.name
  location            = azurerm_resource_group.rg_onboarding.location
}

//federated credential to impersonate when calling onboarding api
resource "azurerm_federated_identity_credential" "onboarding_impersonation_credential" {
  name                = "IdentityServerToAzureAD"
  resource_group_name = azurerm_resource_group.rg_onboarding.name
  audience            = ["api://AzureADTokenExchange"]
  issuer              = "https://${azurerm_container_app.identity_server_app.ingress.0.fqdn}"
  parent_id           = azurerm_user_assigned_identity.external_screening_identity.id
  subject             = azurerm_user_assigned_identity.external_screening_identity.principal_id
}

//api scope assignment to allow access to onboarding api
resource "azuread_app_role_assignment" "external_screening_identity_scope_assignment" {
  app_role_id         = local.onboarding_readwrite_role_id
  principal_object_id = azurerm_user_assigned_identity.external_screening_identity.principal_id
  resource_object_id  = azuread_service_principal.onboarding_api.object_id
}


