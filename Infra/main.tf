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