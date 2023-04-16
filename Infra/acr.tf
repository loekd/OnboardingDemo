//container registry
resource "azurerm_container_registry" "acr" {
  name                = "cronboarding"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Premium"
  admin_enabled       = false
  public_network_access_enabled = true
  network_rule_set = [
    {
      default_action = "Allow"
      ip_rule = [{
        action = "Allow"
        ip_range = var.acr_client_ip
      }]

      virtual_network = []
    }]
}

//enable private link for container registry
resource "azurerm_private_endpoint" "private_endpoint_acr_onboarding" {
  name                = "acr-private-endpoint-onboarding"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
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

//user assigned managed identity to pull container images
resource "azurerm_user_assigned_identity" "acr_pull_identity" {
  name                = "acr-pull-identity"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
}

//role assignment for container registry
resource "azurerm_role_assignment" "acrpull_identity_server_app" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.acr_pull_identity.principal_id
}
