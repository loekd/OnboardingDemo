//adds an azure key vault with a private endpoint
//to store the client secret required to access the screening api from the onboarding app
resource "azurerm_key_vault" "key_vault_onboarding" {
  name                            = "kv-onboarding-001"
  location                        = azurerm_resource_group.rg.location
  resource_group_name             = azurerm_resource_group.rg.name
  enabled_for_disk_encryption     = false
  enabled_for_deployment          = false
  enable_rbac_authorization       = true
  enabled_for_template_deployment = false
  tenant_id                       = data.azurerm_subscription.current.tenant_id
  purge_protection_enabled        = false
  sku_name                        = "standard"

  network_acls {
    default_action             = "Deny"
    bypass                     = "AzureServices"
    ip_rules                   = []
    virtual_network_subnet_ids = []
  }
  tags = {}
}

//private endpoint for key vault
resource "azurerm_private_endpoint" "private_endpoint_key_vault_onboarding" {
  name                = "kv-private-endpoint-onboarding"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  subnet_id           = azurerm_subnet.snet_pe_onboarding.id

  private_dns_zone_group {
    name                 = "kv-private-endpoint-onboarding-zg"
    private_dns_zone_ids = [azurerm_private_dns_zone.private_dns_key_vault.id]
  }

  private_service_connection {
    name                           = "kv-private-endpoint-onboarding-sc"
    private_connection_resource_id = azurerm_key_vault.key_vault_onboarding.id
    subresource_names              = ["vault"]
    is_manual_connection           = false
  }
}

//allow onboarding app to read secrets in the key vault
resource "azurerm_role_assignment" "app_key_vault_reader" {
  scope                = azurerm_key_vault.key_vault_onboarding.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.onboarding_identity.principal_id
}

//DNS zone for key vault private endpoint
resource "azurerm_private_dns_zone" "private_dns_key_vault" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = azurerm_resource_group.rg.name
}

//connect the DNS zone to the vnet
resource "azurerm_private_dns_zone_virtual_network_link" "private_link_key_vault_dns" {
  name                  = "private_link_key_vault_dns"
  resource_group_name   = azurerm_resource_group.rg.name
  private_dns_zone_name = azurerm_private_dns_zone.private_dns_key_vault.name
  virtual_network_id    = azurerm_virtual_network.vnet_onboarding.id
  registration_enabled  = false
}
