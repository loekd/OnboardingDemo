//container app environment
resource "azurerm_container_app_environment" "app_environment_onboarding" {
  name                       = "cae-onboarding"
  location                   = azurerm_resource_group.rg_onboarding.location
  resource_group_name        = azurerm_resource_group.rg_onboarding.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.la.id
  infrastructure_subnet_id   = azurerm_subnet.snet_inbound_onboarding.id
}

resource "azurerm_container_app_environment" "app_environment_screening" {
  name                       = "cae-screening"
  location                   = azurerm_resource_group.rg_screening.location
  resource_group_name        = azurerm_resource_group.rg_screening.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.la.id
  infrastructure_subnet_id   = azurerm_subnet.snet_inbound_screening.id
}

//identity server app
resource "azurerm_container_app" "identity_server_app" {
  name                         = "ca-identity-server"
  container_app_environment_id = azurerm_container_app_environment.app_environment_screening.id
  resource_group_name          = azurerm_resource_group.rg_screening.name
  revision_mode                = "Single"
  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.screening_idp_identity.id]
  }
  registry {
    server   = "cronboarding.azurecr.io"
    identity = azurerm_user_assigned_identity.screening_idp_identity.id
  }
  template {
    container {
      name   = "screening-idp"
      image  = "cronboarding.azurecr.io/externalscreeningidp:${var.identity_server_app_version}"
      cpu    = 0.25
      memory = "0.5Gi"
      env {
        name  = "IdentityServer__ImpersonationIdentityObjectId"
        value = azurerm_user_assigned_identity.external_screening_identity.principal_id
      }
      env {
        name  = "IdentityServer__ClientSecret"
        value = var.screening_client_secret
      }
    }
  }
  ingress {
    allow_insecure_connections = true
    external_enabled           = true
    target_port                = 80
    transport                  = "auto"
    traffic_weight {
      percentage      = 100
      label           = "idp"
      latest_revision = true
    }
  }
  depends_on = [
    azurerm_role_assignment.acrpull_role_screening_idp
  ]
}

//external screening api
resource "azurerm_container_app" "screening_api_app" {
  name                         = "ca-screening-api"
  container_app_environment_id = azurerm_container_app_environment.app_environment_screening.id
  resource_group_name          = azurerm_resource_group.rg_screening.name
  revision_mode                = "Single"
  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.screening_identity.id]
  }
  registry {
    server   = "cronboarding.azurecr.io"
    identity = azurerm_user_assigned_identity.screening_identity.id
  }
  template {
    container {
      name   = "screening-api"
      image  = "cronboarding.azurecr.io/externalscreeningapi:${var.screening_api_app_version}"
      cpu    = 0.25
      memory = "0.5Gi"
      env {
        name  = "IdentityServer__Authority"
        value = "https://${azurerm_container_app.identity_server_app.ingress.0.fqdn}"
      }
      env {
        name  = "OnboardingApi__Authority"
        value = "https://${azurerm_container_app.identity_server_app.ingress.0.fqdn}"
      }
      env {
        name  = "OnboardingApi__Endpoint"
        value = "https://ca-onboarding.${azurerm_container_app_environment.app_environment_onboarding.default_domain}" //chicken & egg problem
      }
      env {
        name  = "OnboardingApi__IdentityServerClientSecret"
        value = var.screening_client_secret
      }
    }
  }
  ingress {
    allow_insecure_connections = true
    external_enabled           = true
    target_port                = 80
    transport                  = "auto"
    traffic_weight {
      percentage      = 100
      label           = "screening"
      latest_revision = true
    }
  }
  depends_on = [
    azurerm_role_assignment.acrpull_role_screening_api
  ]
}

//onboarding app
resource "azurerm_container_app" "onboarding_app" {
  name                         = "ca-onboarding"
  container_app_environment_id = azurerm_container_app_environment.app_environment_onboarding.id
  resource_group_name          = azurerm_resource_group.rg_onboarding.name
  revision_mode                = "Single"
  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.onboarding_identity.id]
  }
  registry {
    server   = "cronboarding.azurecr.io"
    identity = azurerm_user_assigned_identity.onboarding_identity.id
  }
  template {
    container {
      name   = "onboarding-app"
      image  = "cronboarding.azurecr.io/onboardingserver:${var.onboarding_app_version}"
      cpu    = 0.25
      memory = "0.5Gi"
      env {
        name  = "ScreeningApi__Endpoint"
        value = "https://${azurerm_container_app.screening_api_app.ingress.0.fqdn}"
      }
      env {
        name  = "ScreeningApi__Authority"
        value = "https://${azurerm_container_app.identity_server_app.ingress.0.fqdn}"
      }
      env {
        name  = "ScreeningApi__ClientSecret"
        value = var.screening_client_secret
      }
      env {
        name  = "ConnectionStrings__DefaultConnection"
        value = "Server=tcp:${azurerm_mssql_server.sql_server_onboarding.name}.database.windows.net,1433;Initial Catalog=${azurerm_mssql_database.onboarding.name};Authentication='Active Directory Default';Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;Persist Security Info=False;"
      }
      env {
        name  = "AZURE_CLIENT_ID"
        value = azurerm_user_assigned_identity.onboarding_identity.client_id
      }
      env {
        name  = "KeyVault__ClientId"
        value = azurerm_user_assigned_identity.onboarding_identity.client_id
      }
      env {
        name  = "KeyVault__Endpoint"
        value = azurerm_key_vault.key_vault_onboarding.vault_uri
      }
    }
  }
  ingress {
    allow_insecure_connections = true
    external_enabled           = true
    target_port                = 80
    transport                  = "auto"
    traffic_weight {
      percentage      = 100
      label           = "onboarding"
      latest_revision = true
    }
  }
  depends_on = [
    azurerm_role_assignment.acrpull_role_onboarding_app
  ]
}

//user assigned managed identity to impersonate when calling onboarding api from external screening api
resource "azurerm_user_assigned_identity" "external_screening_identity" {
  name                = "external-screening-identity"
  resource_group_name = azurerm_resource_group.rg_onboarding.name
  location            = azurerm_resource_group.rg_onboarding.location
}

//user assigned managed identity for onboarding api
resource "azurerm_user_assigned_identity" "onboarding_identity" {
  name                = "onboarding-identity"
  resource_group_name = azurerm_resource_group.rg_onboarding.name
  location            = azurerm_resource_group.rg_onboarding.location
}

//azure ad user management rights for onboarding identity
resource "azuread_app_role_assignment" "onboarding_user_readwrite_all" {
  app_role_id         = azuread_service_principal.msgraph.app_role_ids["User.ReadWrite.All"]
  principal_object_id = azurerm_user_assigned_identity.onboarding_identity.principal_id
  resource_object_id  = azuread_service_principal.msgraph.object_id
}

//user assigned managed identity for screening api
resource "azurerm_user_assigned_identity" "screening_identity" {
  name                = "screening-identity"
  resource_group_name = azurerm_resource_group.rg_screening.name
  location            = azurerm_resource_group.rg_screening.location
}

//user assigned managed identity for screening idp
resource "azurerm_user_assigned_identity" "screening_idp_identity" {
  name                = "screening--idp-identity"
  resource_group_name = azurerm_resource_group.rg_screening.name
  location            = azurerm_resource_group.rg_screening.location
}
