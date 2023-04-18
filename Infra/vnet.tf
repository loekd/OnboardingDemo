locals {
  address_prefix_onboarding = ["10.0.0.0/16"]
  address_prefix_screening = ["10.128.0.0/16"]
}

//private virtual network
resource "azurerm_virtual_network" "vnet_onboarding" {
  name                = "vnet-onboarding"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  address_space       = local.address_prefix_onboarding
}

//subnet for incoming traffic
resource "azurerm_subnet" "snet_inbound_onboarding" {
name                                               = "snet-inbound-onboarding"
    address_prefixes                               = [for x in local.address_prefix_onboarding : cidrsubnet(x, 5, 0)]
    virtual_network_name                           = azurerm_virtual_network.vnet_onboarding.name
    resource_group_name                            = azurerm_resource_group.rg.name
    private_endpoint_network_policies_enabled      = true
    private_link_service_network_policies_enabled  = true
}

//subnet for private endpoints
resource "azurerm_subnet" "snet_pe_onboarding" {
    name                                           = "snet-private-endpoints-onboarding"
    address_prefixes                               = [for x in local.address_prefix_onboarding : cidrsubnet(x, 5, 1)]
    virtual_network_name                           = azurerm_virtual_network.vnet_onboarding.name
    resource_group_name                            = azurerm_resource_group.rg.name
    private_endpoint_network_policies_enabled      = true
    private_link_service_network_policies_enabled  = true
}

//subnet for outgoing traffic
resource "azurerm_subnet" "snet_outbound_onboarding" {
    name                 = "snet-outbound-onboarding"
    address_prefixes     = [for x in local.address_prefix_onboarding : cidrsubnet(x, 5, 2)]
    virtual_network_name = azurerm_virtual_network.vnet_onboarding.name
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

//association between nat gateway and outbound subnet
resource "azurerm_subnet_nat_gateway_association" "nat_gateway_snet_association" {
  subnet_id      = azurerm_subnet.snet_outbound_onboarding.id
  nat_gateway_id = azurerm_nat_gateway.nat_gateway.id
}

//private dns zones for ACR
resource "azurerm_private_dns_zone" "private_dns_acr_onboarding" {
  name                = "privatelink.azurecr.io"
  resource_group_name = azurerm_resource_group.rg.name
}


resource "azurerm_private_dns_zone_virtual_network_link" "private_link_acr_dns_onboarding" {
  name                  = "private_link_acr_dns_onboarding"
  resource_group_name   = azurerm_resource_group.rg.name
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

//subnet for outgoing traffic
resource "azurerm_subnet" "snet_outbound_screening" {
    name                 = "snet-outbound-screening"
    address_prefixes     = [for x in local.address_prefix_screening : cidrsubnet(x, 5, 2)]
    virtual_network_name = azurerm_virtual_network.vnet_screening.name
    resource_group_name  = azurerm_resource_group.rg_screening.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "private_link_acr_dns_screening" {
  name                  = "private_link_acr_dns_screening"
  resource_group_name   = azurerm_resource_group.rg_screening.name
  private_dns_zone_name = azurerm_private_dns_zone.private_dns_acr_screening.name
  virtual_network_id    = azurerm_virtual_network.vnet_screening.id
  registration_enabled  = false
}
