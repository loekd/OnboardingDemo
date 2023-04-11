//main resource group
resource "azurerm_resource_group" "rg" {
  name     = "Onboarding"
  location = var.location
}

//log analytics workspace
resource "azurerm_log_analytics_workspace" "la" {
  name                = "la-onboarding"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

//container app environment
resource "azurerm_container_app_environment" "app_environment" {
  name                       = "cae-onboarding"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.la.id
  infrastructure_subnet_id   = azurerm_subnet.snet_inbound.id
}

//identity server app
resource "azurerm_container_app" "identity_server_app" {
  name                         = "ca-identity-server"
  container_app_environment_id = azurerm_container_app_environment.app_environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"
  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.acr_pull_identity.id]
  }
  registry {
    server   = "cronboarding.azurecr.io"
    identity = azurerm_user_assigned_identity.acr_pull_identity.id
  }
  template {
    container {
      name   = "screening-idp"
      image  = "cronboarding.azurecr.io/externalscreeningidp:${var.identity_server_app_version}"
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }
  ingress {
    allow_insecure_connections = true
    external_enabled           = true
    target_port                = 80
    transport                  = "auto"
    traffic_weight {
      percentage = 100
      label = "idp"
      latest_revision = true
    }
  }
  depends_on = [
    azurerm_role_assignment.acrpull_identity_server_app
  ]
}

//external screening api
resource "azurerm_container_app" "screening_api_app" {
  name                         = "ca-screening-api"
  container_app_environment_id = azurerm_container_app_environment.app_environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"
  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.acr_pull_identity.id]
  }
  registry {
    server   = "cronboarding.azurecr.io"
    identity = azurerm_user_assigned_identity.acr_pull_identity.id
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
    }
  }
  ingress {
    allow_insecure_connections = true
    external_enabled           = true
    target_port                = 80
    transport                  = "auto"
    traffic_weight {
      percentage = 100
      label = "screening"
      latest_revision = true
    }
  }
  depends_on = [
    azurerm_role_assignment.acrpull_identity_server_app
  ]
}

//onboarding app
resource "azurerm_container_app" "onboarding_app" {
  name                         = "ca-onboarding"
  container_app_environment_id = azurerm_container_app_environment.app_environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"
  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.acr_pull_identity.id]
  }
  registry {
    server   = "cronboarding.azurecr.io"
    identity = azurerm_user_assigned_identity.acr_pull_identity.id
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
    }
  }
  ingress {
    allow_insecure_connections = true
    external_enabled           = true
    target_port                = 80
    transport                  = "auto"
    traffic_weight {
      percentage = 100
      label = "onboarding"
      latest_revision = true
    }
  }
  depends_on = [
    azurerm_role_assignment.acrpull_identity_server_app
  ]
}

//user assigned managed identity
resource "azurerm_user_assigned_identity" "acr_pull_identity" {
  name                = "acr-pull-identity"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
}
