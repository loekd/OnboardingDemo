locals {
  address_prefix = ["10.0.0.0/16"]
}

//private virtual network
resource "azurerm_virtual_network" "vnet" {
  name                = "vnet-onboarding"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  address_space       = local.address_prefix
}

//subnet for incoming traffic
resource "azurerm_subnet" "snet_inbound" {
name                                               = "snet-inbound"
    address_prefixes                               = [for x in local.address_prefix : cidrsubnet(x, 5, 0)]
    virtual_network_name                           = azurerm_virtual_network.vnet.name
    resource_group_name                            = azurerm_resource_group.rg.name
    private_endpoint_network_policies_enabled      = true
    private_link_service_network_policies_enabled  = true
}

//subnet for private endpoints
resource "azurerm_subnet" "snet_pe" {
    name                                           = "snet-private-endpoints"
    address_prefixes                               = [for x in local.address_prefix : cidrsubnet(x, 5, 1)]
    virtual_network_name                           = azurerm_virtual_network.vnet.name
    resource_group_name                            = azurerm_resource_group.rg.name
    private_endpoint_network_policies_enabled      = true
    private_link_service_network_policies_enabled  = true
}

//subnet for outgoing traffic
resource "azurerm_subnet" "snet_outbound" {
    name                 = "snet-outbound"
    address_prefixes     = [for x in local.address_prefix : cidrsubnet(x, 5, 2)]
    virtual_network_name = azurerm_virtual_network.vnet.name
    resource_group_name  = azurerm_resource_group.rg.name
}

//nat gateway for outbound traffic
resource "azurerm_nat_gateway" "nat_gateway" {
  name                = "ng-onboarding"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  idle_timeout_in_minutes = 4
  sku_name = "Standard"
  
}

//public IP for nat gateway
resource "azurerm_public_ip" "nat_gateway_pip" {
  name                = "pip-nat-gateway"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  allocation_method   = "Static"
  sku                 = "Standard"
}

//association between nat gateway and public IP
resource "azurerm_nat_gateway_public_ip_association" "nat_gateway_public_ip_association" {
  nat_gateway_id       = azurerm_nat_gateway.nat_gateway.id
  public_ip_address_id = azurerm_public_ip.nat_gateway_pip.id
}

//private dns zone for ACR
resource "azurerm_private_dns_zone" "private_dns_acr" {
  name                = "privatelink.azurecr.io"
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "private_link_acr_dns" {
  name                  = "private_link_acr_dns"
  resource_group_name   = azurerm_resource_group.rg.name
  private_dns_zone_name = azurerm_private_dns_zone.private_dns_acr.name
  virtual_network_id    = azurerm_virtual_network.vnet.id
  registration_enabled  = false
}