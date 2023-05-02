locals {
  address_prefix_onboarding = ["10.0.0.0/16"]
  address_prefix_screening = ["10.128.0.0/16"]
}

//private virtual network
resource "azurerm_virtual_network" "vnet_onboarding" {
  name                = "vnet-onboarding"
  location            = azurerm_resource_group.rg_onboarding.location
  resource_group_name = azurerm_resource_group.rg_onboarding.name
  address_space       = local.address_prefix_onboarding
}

//subnet for incoming traffic
resource "azurerm_subnet" "snet_inbound_onboarding" {
name                                               = "snet-inbound-onboarding"
    address_prefixes                               = [for x in local.address_prefix_onboarding : cidrsubnet(x, 5, 0)]
    virtual_network_name                           = azurerm_virtual_network.vnet_onboarding.name
    resource_group_name                            = azurerm_resource_group.rg_onboarding.name
    private_endpoint_network_policies_enabled      = true
    private_link_service_network_policies_enabled  = true
}

//subnet for private endpoints
resource "azurerm_subnet" "snet_pe_onboarding" {
    name                                           = "snet-private-endpoints-onboarding"
    address_prefixes                               = [for x in local.address_prefix_onboarding : cidrsubnet(x, 5, 1)]
    virtual_network_name                           = azurerm_virtual_network.vnet_onboarding.name
    resource_group_name                            = azurerm_resource_group.rg_onboarding.name
    private_endpoint_network_policies_enabled      = true
    private_link_service_network_policies_enabled  = true
}

//private dns zones for ACR
resource "azurerm_private_dns_zone" "private_dns_acr_onboarding" {
  name                = "privatelink.azurecr.io"
  resource_group_name = azurerm_resource_group.rg_onboarding.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "private_link_acr_dns_onboarding" {
  name                  = "private_link_acr_dns_onboarding"
  resource_group_name   = azurerm_resource_group.rg_onboarding.name
  private_dns_zone_name = azurerm_private_dns_zone.private_dns_acr_onboarding.name
  virtual_network_id    = azurerm_virtual_network.vnet_onboarding.id
  registration_enabled  = false
}


/////////////////////screening

resource "azurerm_private_dns_zone" "private_dns_acr_screening" {
  name                = "privatelink.azurecr.io"
  resource_group_name = azurerm_resource_group.rg_screening.name
}

resource "azurerm_virtual_network" "vnet_screening" {
  name                = "vnet-screening"
  location            = azurerm_resource_group.rg_screening.location
  resource_group_name = azurerm_resource_group.rg_screening.name
  address_space       = local.address_prefix_screening
}


//subnet for incoming traffic
resource "azurerm_subnet" "snet_inbound_screening" {
name                                               = "snet-inbound-screening"
    address_prefixes                               = [for x in local.address_prefix_screening : cidrsubnet(x, 5, 0)]
    virtual_network_name                           = azurerm_virtual_network.vnet_screening.name
    resource_group_name                            = azurerm_resource_group.rg_screening.name
    private_endpoint_network_policies_enabled      = true
    private_link_service_network_policies_enabled  = true
}

//subnet for private endpoints
resource "azurerm_subnet" "snet_pe_screening" {
    name                                           = "snet-private-endpoints-screening"
    address_prefixes                               = [for x in local.address_prefix_screening : cidrsubnet(x, 5, 1)]
    virtual_network_name                           = azurerm_virtual_network.vnet_screening.name
    resource_group_name                            = azurerm_resource_group.rg_screening.name
    private_endpoint_network_policies_enabled      = true
    private_link_service_network_policies_enabled  = true
}


resource "azurerm_private_dns_zone_virtual_network_link" "private_link_acr_dns_screening" {
  name                  = "private_link_acr_dns_screening"
  resource_group_name   = azurerm_resource_group.rg_screening.name
  private_dns_zone_name = azurerm_private_dns_zone.private_dns_acr_screening.name
  virtual_network_id    = azurerm_virtual_network.vnet_screening.id
  registration_enabled  = false
}
