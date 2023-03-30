//main resource group
resource "azurerm_resource_group" "rg" {
  name     = "Onboarding"
  location = "West Europe"
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
  infrastructure_subnet_id = azurerm_subnet.snet_inbound.id
}

//identity server app
resource "azurerm_container_app" "identity_server_app" {
  name                         = "ca-identity-server"
  container_app_environment_id = azurerm_container_app_environment.app_environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"
  identity {
    type = "SystemAssigned"
  }

  template {
    container {
      name   = "examplecontainerapp"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }
}

//onboarding app
resource "azurerm_container_app" "onboarding_app" {
  name                         = "ca-onboarding"
  container_app_environment_id = azurerm_container_app_environment.app_environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"
  identity {
    type = "SystemAssigned"
  }

  template {
    container {
      name   = "examplecontainerapp"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }
}
