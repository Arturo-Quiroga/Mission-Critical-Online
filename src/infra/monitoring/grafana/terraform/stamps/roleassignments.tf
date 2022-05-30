# Permission for App Services to pull images from the globally shared ACR
resource "azurerm_role_assignment" "acrpull_role" {
  for_each             = var.stamps
  scope                = var.acr_resource_id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_linux_web_app.appservice[each.key].identity[0].principal_id
}

# Permission for Grafana to read from all Log Analytics workspaces in the subscription
resource "azurerm_role_assignment" "loganalyticsreader_role" {
  for_each             = var.stamps
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "Log Analytics Reader"
  principal_id         = azurerm_linux_web_app.appservice[each.key].identity[0].principal_id
}

# user assigned managed identity
resource "azurerm_user_assigned_identity" "loganalytics_reader_mi" {
  for_each = var.stamps

  location            = azurerm_resource_group.rg[each.key].location
  resource_group_name = azurerm_resource_group.rg[each.key].name

  name = "loganalytics-reader"
}

resource "azurerm_role_assignment" "loganalytics_reader_mi_role" {
  for_each             = var.stamps
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "Log Analytics Reader"
  principal_id         = azurerm_user_assigned_identity.loganalytics_reader_mi[each.key].principal_id
}

resource "azurerm_role_assignment" "acrpull_role_umi" {
  for_each             = var.stamps
  scope                = var.acr_resource_id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.loganalytics_reader_mi[each.key].principal_id
}
