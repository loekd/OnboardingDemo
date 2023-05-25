//container registry
resource "azurerm_container_registry" "acr" {
  name                          = "cronboarding"
  resource_group_name           = azurerm_resource_group.rg_onboarding.name
  location                      = azurerm_resource_group.rg_onboarding.location
  sku                           = "Premium"
  admin_enabled                 = false
  public_network_access_enabled = true
  identity {
    type = "SystemAssigned"
  }

  network_rule_set = [
    {
      default_action = "Allow"
      ip_rule = [{
        action   = "Allow"
        ip_range = var.acr_client_ip
      }]

      virtual_network = []
  }]
}

//enable private link for container registry
resource "azurerm_private_endpoint" "private_endpoint_acr_onboarding" {
  name                = "acr-private-endpoint-onboarding"
  location            = azurerm_resource_group.rg_onboarding.location
  resource_group_name = azurerm_resource_group.rg_onboarding.name
  subnet_id           = azurerm_subnet.snet_pe_onboarding.id

  private_dns_zone_group {
    name                 = "acr-private-endpoint_zg"
    private_dns_zone_ids = [azurerm_private_dns_zone.private_dns_acr_onboarding.id]
  }

  private_service_connection {
    name                           = "pe-acr"
    private_connection_resource_id = azurerm_container_registry.acr.id
    subresource_names              = ["registry"]
    is_manual_connection           = false
  }
}

//enable private link for container registry
resource "azurerm_private_endpoint" "private_endpoint_acr_screening" {
  name                = "acr-private-endpoint-screening"
  location            = azurerm_resource_group.rg_screening.location
  resource_group_name = azurerm_resource_group.rg_screening.name
  subnet_id           = azurerm_subnet.snet_pe_screening.id

  private_dns_zone_group {
    name                 = "acr-private-endpoint_zg"
    private_dns_zone_ids = [azurerm_private_dns_zone.private_dns_acr_screening.id]
  }

  private_service_connection {
    name                           = "pe-acr"
    private_connection_resource_id = azurerm_container_registry.acr.id
    subresource_names              = ["registry"]
    is_manual_connection           = false
  }
}


//role assignment for container registry
resource "azurerm_role_assignment" "acrpull_role_onboarding_app" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.onboarding_identity.principal_id
}

//role assignment for container registry
resource "azurerm_role_assignment" "acrpull_role_screening_api" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.screening_identity.principal_id
}

//role assignment for container registry
resource "azurerm_role_assignment" "acrpull_role_screening_idp" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.screening_idp_identity.principal_id
}

