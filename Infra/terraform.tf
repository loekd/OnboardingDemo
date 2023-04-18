# We use different providers for passing subscriptions guids throught to the.
# More information can be found https://www.terraform.io/docs/configuration/modules.html

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.49.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "=2.8.0"
    }
    mssql = {
      source  = "betr-io/mssql"
      version = "0.1.0"
    }
  }
  backend "azurerm" {
    resource_group_name  = "OnboardingState"
    storage_account_name = "stonboardingtfstate"
    container_name       = "tfstate"
    key                  = "infra.tfstate"
  }
}

