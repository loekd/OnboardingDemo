//role assignment for container registry
resource "azurerm_role_assignment" "acrpull_identity_server_app" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.acr_pull_identity.principal_id
}


//container registry
resource "azurerm_container_registry" "acr" {
  name                = "cronboarding"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Premium"
  admin_enabled       = false
  public_network_access_enabled = true
}

//enable private link for container registry
resource "azurerm_private_endpoint" "private_endpoint_acr" {
  name                = "acr-private-endpoint"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  subnet_id           = azurerm_subnet.snet_pe.id

  private_dns_zone_group {
    name                 = "acr-private-endpoint_zg"
    private_dns_zone_ids = [azurerm_private_dns_zone.private_dns_acr.id]
  }

  private_service_connection {
    name                           = "pe-acr"
    private_connection_resource_id = azurerm_container_registry.acr.id
    subresource_names              = ["registry"]
    is_manual_connection           = false
  }
}