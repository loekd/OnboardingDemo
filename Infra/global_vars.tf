locals {
  customer                     = terraform.workspace != "" ? element(split("-", terraform.workspace), 0) : "default"
  environment                  = terraform.workspace != "" ? element(split("-", terraform.workspace), 1) : "workspace"
  log_analytics_name           = "log-${lower(local.customer)}-${local.environment}"
  aks_name                     = "aks-${local.customer}-${local.environment}"
  acr_name                     = "cr${lower(local.customer)}${local.environment}"
  service_bus_name             = "sb-${lower(local.customer)}-${local.environment}"
  vnet_name                    = "vnet-${local.customer}-${local.environment}"
  runtime_shared_vnet_name     = "vnet-${local.customer}-runtime-shared"
  baseline_resourcegroup       = "rg-${local.customer}-baseline-${local.environment}"
  compute_resourcegroup        = "rg-${local.customer}-compute-${local.environment}"
  storage_resourcegroup        = "rg-${local.customer}-storage-${local.environment}"
  runtime_shared_resourcegroup = "rg-${local.customer}-runtime-shared-${local.environment}"
  sql_server_name              = "sql-${lower(local.customer)}-${local.environment}"
  sql_database_name            = "sqldb-${local.customer}-${local.environment}"
  key_vault_infra_name         = "kv-infra-${lower(local.customer)}-${local.environment}"
  key_vault_app_name           = "kv-app-${lower(local.customer)}-${local.environment}"
  storage_account_name         = "st${lower(local.customer)}${local.environment}"
  tenant_id                    = data.azurerm_subscription.current.tenant_id
  cluster_subnet_name          = "snet-cluster"
}

data "azurerm_subscription" "current" {}
