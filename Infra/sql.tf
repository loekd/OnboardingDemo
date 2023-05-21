data "azuread_client_config" "current" {}
data "azuread_application_published_app_ids" "well_known" {}

resource "random_password" "onboarding_password" {
  length           = 16
  special          = true
  override_special = "!#$()-_=+[]{}:?"
}

resource "azurerm_mssql_server" "sql_server_onboarding" {
  name                              = "sql-onboarding-demo-001"
  resource_group_name               = azurerm_resource_group.rg_onboarding.name
  location                          = azurerm_resource_group.rg_onboarding.location
  version                           = "12.0"
  administrator_login               = "azureadmin"
  administrator_login_password      = random_password.onboarding_password.result
  primary_user_assigned_identity_id = azurerm_user_assigned_identity.onboarding_db_identity.id
  identity {
    type = "UserAssigned"
    identity_ids = [
      azurerm_user_assigned_identity.onboarding_db_identity.id
    ]
  }

  azuread_administrator {
    login_username = azuread_group.db_admins.display_name
    object_id      = azuread_group.db_admins.object_id
  }

  depends_on = [
    azuread_app_role_assignment.onboarding_application_read_all,
    azuread_app_role_assignment.onboarding_groupmember_read_all,
    azuread_app_role_assignment.onboarding_user_read_all
  ]
}

resource "azurerm_mssql_database" "onboarding" {
  name           = "sqldb-onboarding"
  server_id      = azurerm_mssql_server.sql_server_onboarding.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  max_size_gb    = 1
  sku_name       = "Basic"
  zone_redundant = false
}

resource "azurerm_private_endpoint" "pe_sql_onboarding" {
  name                = "pe-onboarding"
  location            = azurerm_resource_group.rg_onboarding.location
  resource_group_name = azurerm_resource_group.rg_onboarding.name
  subnet_id           = azurerm_subnet.snet_pe_onboarding.id

  private_dns_zone_group {
    name                 = "sql-private-endpoint_zg"
    private_dns_zone_ids = [azurerm_private_dns_zone.private_dns_sql_onboarding.id]
  }

  private_service_connection {
    name                           = "psc_onboarding"
    private_connection_resource_id = azurerm_mssql_server.sql_server_onboarding.id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = false
  }
}

resource "azurerm_private_dns_zone" "private_dns_sql_onboarding" {
  name                = "privatelink.database.windows.net"
  resource_group_name = azurerm_resource_group.rg_onboarding.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "link__private_dns_sql_onboarding" {
  name                  = "vnetlink_onboarding"
  resource_group_name   = azurerm_resource_group.rg_onboarding.name
  private_dns_zone_name = azurerm_private_dns_zone.private_dns_sql_onboarding.name
  virtual_network_id    = azurerm_virtual_network.vnet_onboarding.id
}


resource "azuread_app_role_assignment" "onboarding_user_read_all" {
  app_role_id         = azuread_service_principal.msgraph.app_role_ids["User.Read.All"]
  principal_object_id = azurerm_user_assigned_identity.onboarding_db_identity.principal_id
  resource_object_id  = azuread_service_principal.msgraph.object_id
}

resource "azuread_app_role_assignment" "onboarding_groupmember_read_all" {
  app_role_id         = azuread_service_principal.msgraph.app_role_ids["GroupMember.Read.All"]
  principal_object_id = azurerm_user_assigned_identity.onboarding_db_identity.principal_id
  resource_object_id  = azuread_service_principal.msgraph.object_id
}

resource "azuread_app_role_assignment" "onboarding_application_read_all" {
  app_role_id         = azuread_service_principal.msgraph.app_role_ids["Application.Read.All"]
  principal_object_id = azurerm_user_assigned_identity.onboarding_db_identity.principal_id
  resource_object_id  = azuread_service_principal.msgraph.object_id
}


resource "azuread_service_principal" "msgraph" {
  application_id = data.azuread_application_published_app_ids.well_known.result.MicrosoftGraph
  use_existing   = true
}

//spn for onboarding admin
resource "azuread_application" "onboarding_admin_app" {
  display_name = "OnboardingAdmin"
  owners       = [data.azuread_client_config.current.object_id]
  lifecycle {
    ignore_changes = [required_resource_access]
  }
}

resource "azuread_service_principal" "onboarding_admin_spn" {
  application_id               = azuread_application.onboarding_admin_app.application_id
  app_role_assignment_required = false
  owners                       = [data.azuread_client_config.current.object_id]
}

resource "azuread_service_principal_password" "onboarding_admin_spn_password" {
  service_principal_id = azuread_service_principal.onboarding_admin_spn.object_id
}

//Reader role assignment for subscription for SQL Admin SPN
resource "azurerm_role_assignment" "subscription_reader_spn" {
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "Reader"
  principal_id         = azuread_service_principal.onboarding_admin_spn.object_id
}

//SQL Admins AAD security group
resource "azuread_group" "db_admins" {
  display_name     = "db_admins"
  owners           = [data.azuread_client_config.current.object_id]
  security_enabled = true
  members = [
    data.azuread_client_config.current.object_id,
    azuread_service_principal.onboarding_admin_spn.object_id
  ]
}

//SQL login in master database

locals {
  login             = azurerm_user_assigned_identity.onboarding_identity.name
  connection_string = sensitive("data source=${azurerm_mssql_server.sql_server_onboarding.name}.database.windows.net;initial catalog=master;encrypt=True;connection timeout=30;")
  query             = <<EOT
    IF(EXISTS (SELECT * FROM sys.sql_logins WHERE name = '${local.login}'))
    BEGIN
      DROP LOGIN [${local.login}]
    END
    CREATE LOGIN [${local.login}] FROM EXTERNAL PROVIDER;
    EOT
}

#create login
resource "null_resource" "create_sql_login" {

  # triggers = {
  #   always_run = "${timestamp()}"
  # }

  provisioner "local-exec" {
    environment = {
      pwd    = azuread_service_principal_password.onboarding_admin_spn_password.value,
      usr    = azuread_application.onboarding_admin_app.application_id,
      tenant = data.azurerm_subscription.current.tenant_id,
      sub    = data.azurerm_subscription.current.subscription_id
    }

    command     = <<EOT

    #login as current SPN
    $password = "$env:pwd" | ConvertTo-SecureString -AsPlainText -Force
    $appId = "$env:usr"
    $tenant = "$env:tenant"
    $subscriptionId = "$env:sub"
    $credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $appId, $password
    Connect-AzAccount -ServicePrincipal -TenantId $tenant -Credential $credential -SubscriptionId $subscriptionId

    #execute query
    $token = Get-AzAccessToken -ResourceUrl 'https://database.windows.net/'
    $sqlConnection = New-Object System.Data.SqlClient.sqlConnection                
    $sqlCmd = New-Object System.Data.SqlClient.SqlCommand
    $sqlCmd.CommandText = "${local.query}"
    $sqlConnection.ConnectionString = "${local.connection_string}"
    $sqlConnection.AccessToken = $token.Token
    try{
      $sqlConnection.Open()
      $sqlCmd.Connection = $sqlConnection 
      $sqlCmd.ExecuteNonQuery()
    }
    finally{
      $sqlCmd.Dispose()
      $sqlConnection.Dispose()
    }
    EOT
    interpreter = ["pwsh", "-Command"]
  }

  depends_on = [
    azurerm_mssql_server.sql_server_onboarding,
    azurerm_role_assignment.subscription_reader_spn,
    azuread_app_role_assignment.onboarding_user_read_all,
    azuread_app_role_assignment.onboarding_groupmember_read_all,
    azuread_app_role_assignment.onboarding_application_read_all
  ]
}


///SQL user in app database

locals {
  connection_string_user = sensitive("data source=${azurerm_mssql_server.sql_server_onboarding.name}.database.windows.net;initial catalog=${azurerm_mssql_database.onboarding.name};encrypt=True;connection timeout=30;")
  query_user             = <<EOT
    IF (EXISTS (SELECT name FROM sys.database_principals WHERE name = '${local.login}'))
    BEGIN
      DROP USER [${local.login}]
    END
    CREATE USER [${local.login}] FROM LOGIN [${local.login}]; 
    ALTER ROLE [db_datareader] ADD MEMBER [${local.login}];  
    ALTER ROLE [db_datawriter] ADD MEMBER [${local.login}];
    ALTER ROLE [db_ddladmin] ADD MEMBER [${local.login}];
    
    EOT
}

#create database user and allow it access to the database
resource "null_resource" "create_sql_user_and_roles" {

  # triggers = {
  #   always_run = "${timestamp()}"
  # }

  provisioner "local-exec" {
    environment = {
      pwd    = azuread_service_principal_password.onboarding_admin_spn_password.value,
      usr    = azuread_application.onboarding_admin_app.application_id,
      tenant = data.azurerm_subscription.current.tenant_id,
      sub    = data.azurerm_subscription.current.subscription_id
    }

    command     = <<EOT

    #login as current SPN
    $password = "$env:pwd" | ConvertTo-SecureString -AsPlainText -Force
    $appId = "$env:usr"
    $tenant = "$env:tenant"
    $subscriptionId = "$env:sub"
    $credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $appId, $password
    Connect-AzAccount -ServicePrincipal -TenantId $tenant -Credential $credential -SubscriptionId $subscriptionId

    #execute query
    $token = Get-AzAccessToken -ResourceUrl 'https://database.windows.net/'
    $sqlConnection = New-Object System.Data.SqlClient.sqlConnection                
    $sqlCmd = New-Object System.Data.SqlClient.SqlCommand
    $sqlCmd.CommandText = "${local.query_user}"
    $sqlConnection.ConnectionString = "${local.connection_string_user}"
    $sqlConnection.AccessToken = $token.Token
    try{
      $sqlConnection.Open()
      $sqlCmd.Connection = $sqlConnection 
      $sqlCmd.ExecuteNonQuery()
    }
    finally{
      $sqlCmd.Dispose()
      $sqlConnection.Dispose()
    }
    EOT
    interpreter = ["pwsh", "-Command"]
  }

  depends_on = [
    azurerm_mssql_database.onboarding,
    null_resource.create_sql_login
  ]
}
