using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class InventoryWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<InventoryItem> _allInventoryItems;
        private List<LocationInventoryItem> _allLocationInventoryItems;
        private List<InventoryTransaction> _allTransactions;
        private List<InventoryAlert> _allAlerts;
        private List<Location> _allLocations;
        private InventoryItem _selectedInventoryItem;
        private LocationInventoryItem _selectedLocationInventoryItem;
        private InventoryAlert _selectedAlert;

        public InventoryWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            InitializeDatePickers();
            LoadLocations();
            LoadInventoryData();
            LoadLocationInventoryData();
            LoadTransactions();
            LoadAlerts();
        }

        private void InitializeDatePickers()
        {
            dpStartDate.SelectedDate = DateTime.Today.AddDays(-30);
            dpEndDate.SelectedDate = DateTime.Today;
        }

        private void LoadLocations()
        {
            try
            {
                _allLocations = GetAllLocations();
                
                // Populate location filter dropdowns
                cmbLocationFilter.Items.Clear();
                cmbLocationFilter.Items.Add(new ComboBoxItem { Content = "All Locations", IsSelected = true });
                
                cmbLocationFilter2.Items.Clear();
                cmbLocationFilter2.Items.Add(new ComboBoxItem { Content = "All Locations", IsSelected = true });
                
                foreach (var location in _allLocations)
                {
                    cmbLocationFilter.Items.Add(new ComboBoxItem { Content = location.Name });
                    cmbLocationFilter2.Items.Add(new ComboBoxItem { Content = location.Name });
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading locations:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Location Error");
            }
        }

        private List<Location> GetAllLocations()
        {
            return _databaseContext.GetAllLocations();
        }

        #region Stock Overview Tab

        private void LoadInventoryData()
        {
            try
            {
                _allInventoryItems = GetAllInventoryItems();
                dgInventory.ItemsSource = _allInventoryItems;
                UpdateSummaryCards();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading inventory data:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Inventory Error");
            }
        }

        private List<InventoryItem> GetAllInventoryItems()
        {
            var items = new List<InventoryItem>();
            var locations = _databaseContext.GetAllLocations();
            
            // Get all products and calculate their stock across all locations
            var products = _databaseContext.GetAllProducts();
            foreach (var product in products.Where(p => p.IsActive))
            {
                var totalStock = 0;
                var totalMinStock = 0;
                
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    var productStock = _databaseContext.GetProductStock(product.Id, location.Id);
                    var minStock = _databaseContext.GetMinimumStock("Product", product.Id, location.Id);
                    
                    totalStock += productStock;
                    totalMinStock += minStock;
                }
                
                items.Add(new InventoryItem
                {
                    Id = product.Id,
                    SKU = product.SKU,
                    Name = product.Name,
                    Description = product.Description ?? "",
                    ItemType = "Product",
                    Price = product.Price,
                    CurrentStock = totalStock,
                    MinimumStock = totalMinStock,
                    IsActive = product.IsActive,
                    LastModified = product.LastModified
                });
            }
            
            // Get all components and calculate their stock across all locations
            var components = _databaseContext.GetAllComponents();
            foreach (var component in components.Where(c => c.IsActive))
            {
                var totalStock = 0;
                var totalMinStock = 0;
                
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    var componentStock = _databaseContext.GetComponentStock(component.Id, location.Id);
                    var minStock = _databaseContext.GetMinimumStock("Component", component.Id, location.Id);
                    
                    totalStock += componentStock;
                    totalMinStock += minStock;
                }
                
                items.Add(new InventoryItem
                {
                    Id = component.Id,
                    SKU = component.SKU,
                    Name = component.Name,
                    Description = component.Description ?? "",
                    ItemType = "Component",
                    Cost = component.Cost,
                    CurrentStock = totalStock,
                    MinimumStock = totalMinStock,
                    IsActive = component.IsActive,
                    LastModified = component.LastModified
                });
            }
            
            return items;
        }

        private void UpdateSummaryCards()
        {
            if (_allInventoryItems == null) return;

            var totalProducts = _allInventoryItems.Count(x => x.ItemType == "Product");
            var totalComponents = _allInventoryItems.Count(x => x.ItemType == "Component");
            var lowStockItems = _allInventoryItems.Count(x => x.StockStatus == "Low Stock");
            var outOfStockItems = _allInventoryItems.Count(x => x.StockStatus == "Out of Stock");

            txtTotalProducts.Text = totalProducts.ToString();
            txtTotalComponents.Text = totalComponents.ToString();
            txtLowStockItems.Text = lowStockItems.ToString();
            txtOutOfStock.Text = outOfStockItems.ToString();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInventoryData();
        }

        private void btnAddTransaction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addTransactionWindow = new AddInventoryTransactionWindow(_databaseContext);
                if (addTransactionWindow.ShowDialog() == true)
                {
                    LoadInventoryData();
                    LoadLocationInventoryData();
                    LoadTransactions();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening add transaction window: {ex.Message}", "Error");
            }
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var transferWindow = new InventoryTransferWindow(_databaseContext);
                if (transferWindow.ShowDialog() == true)
                {
                    LoadInventoryData();
                    LoadLocationInventoryData();
                    LoadTransactions();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening transfer window: {ex.Message}", "Error");
            }
        }

        private void btnGeneratePO_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement purchase order generation
            ErrorDialog.ShowInformation("Purchase order generation feature will be implemented soon.", "Feature Coming Soon");
        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyInventoryFilter();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyInventoryFilter();
        }

        private void cmbLocationFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyInventoryFilter();
        }

        private void ApplyInventoryFilter()
        {
            if (_allInventoryItems == null) return;

            var searchText = txtSearch.Text.ToLower();
            var filterText = (cmbFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            var locationFilter = (cmbLocationFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

            var filteredItems = _allInventoryItems.Where(item =>
            {
                // Search filter
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                  item.Name.ToLower().Contains(searchText) ||
                                  item.SKU.ToLower().Contains(searchText) ||
                                  item.Description.ToLower().Contains(searchText);

                // Type filter
                var matchesFilter = filterText switch
                {
                    "Low Stock" => item.StockStatus == "Low Stock",
                    "Out of Stock" => item.StockStatus == "Out of Stock",
                    "Products Only" => item.ItemType == "Product",
                    "Components Only" => item.ItemType == "Component",
                    _ => true // "All Items"
                };

                // Location filter (for now, show all items since we're in the overview tab)
                var matchesLocation = locationFilter == "All Locations" || locationFilter == null;

                return matchesSearch && matchesFilter && matchesLocation;
            }).ToList();

            dgInventory.ItemsSource = filteredItems;
        }

        private void dgInventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedInventoryItem = dgInventory.SelectedItem as InventoryItem;
        }

        #endregion

        #region Location Inventory Tab

        private void LoadLocationInventoryData()
        {
            try
            {
                _allLocationInventoryItems = GetAllLocationInventoryItems();
                dgLocationInventory.ItemsSource = _allLocationInventoryItems;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading location inventory data:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Location Inventory Error");
            }
        }

        private List<LocationInventoryItem> GetAllLocationInventoryItems()
        {
            var items = new List<LocationInventoryItem>();
            
            using var connection = _databaseContext.GetConnection();
            
            // Get Products with location breakdown
            var productSql = @"
                SELECT p.Id, p.SKU, p.Name, p.Description, p.Price, p.IsActive, p.LastModified,
                       COALESCE(SUM(li.CurrentStock) OVER (PARTITION BY p.Id), 0) as TotalStock,
                       l.Id as LocationId, l.Name as LocationName,
                       COALESCE(li.CurrentStock, 0) as LocationStock,
                       COALESCE(li.MinimumStock, 0) as MinimumStock
                FROM Products p
                LEFT JOIN LocationInventory li ON p.Id = li.ItemId AND li.ItemType = 'Product'
                LEFT JOIN Locations l ON li.LocationId = l.Id
                WHERE p.IsActive = 1
                ORDER BY p.Name, l.Name";
            
            using var productCommand = new MySqlCommand(productSql, connection);
            using var productReader = productCommand.ExecuteReader();
            
            while (productReader.Read())
            {
                items.Add(new LocationInventoryItem
                {
                    Id = productReader.GetInt32(0),
                    SKU = productReader.GetString(1),
                    Name = productReader.GetString(2),
                    Description = productReader.IsDBNull(3) ? "" : productReader.GetString(3),
                    ItemType = "Product",
                    Price = productReader.GetDecimal(4),
                    IsActive = productReader.GetBoolean(5),
                    LastModified = productReader.GetDateTime(6),
                    TotalStockAcrossLocations = productReader.GetInt32(7),
                    LocationId = productReader.IsDBNull(8) ? 0 : productReader.GetInt32(8),
                    LocationName = productReader.IsDBNull(9) ? "No Location" : productReader.GetString(9),
                    CurrentStock = productReader.GetInt32(10),
                    MinimumStock = productReader.GetInt32(11)
                });
            }
            
            productReader.Close();
            
            // Get Components with location breakdown
            var componentSql = @"
                SELECT c.Id, c.SKU, c.Name, c.Description, c.Cost, c.IsActive, c.LastModified,
                       COALESCE(SUM(li.CurrentStock) OVER (PARTITION BY c.Id), 0) as TotalStock,
                       l.Id as LocationId, l.Name as LocationName,
                       COALESCE(li.CurrentStock, 0) as LocationStock,
                       COALESCE(li.MinimumStock, 0) as MinimumStock
                FROM Components c
                LEFT JOIN LocationInventory li ON c.Id = li.ItemId AND li.ItemType = 'Component'
                LEFT JOIN Locations l ON li.LocationId = l.Id
                WHERE c.IsActive = 1
                ORDER BY c.Name, l.Name";
            
            using var componentCommand = new MySqlCommand(componentSql, connection);
            using var componentReader = componentCommand.ExecuteReader();
            
            while (componentReader.Read())
            {
                items.Add(new LocationInventoryItem
                {
                    Id = componentReader.GetInt32(0),
                    SKU = componentReader.GetString(1),
                    Name = componentReader.GetString(2),
                    Description = componentReader.IsDBNull(3) ? "" : componentReader.GetString(3),
                    ItemType = "Component",
                    Cost = componentReader.GetDecimal(4),
                    IsActive = componentReader.GetBoolean(5),
                    LastModified = componentReader.GetDateTime(6),
                    TotalStockAcrossLocations = componentReader.GetInt32(7),
                    LocationId = componentReader.IsDBNull(8) ? 0 : componentReader.GetInt32(8),
                    LocationName = componentReader.IsDBNull(9) ? "No Location" : componentReader.GetString(9),
                    CurrentStock = componentReader.GetInt32(10),
                    MinimumStock = componentReader.GetInt32(11)
                });
            }
            
            return items;
        }

        private void btnRefreshLocationInventory_Click(object sender, RoutedEventArgs e)
        {
            LoadLocationInventoryData();
        }

        private void btnTransferBetweenLocations_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var transferWindow = new InventoryTransferWindow(_databaseContext);
                if (transferWindow.ShowDialog() == true)
                {
                    LoadInventoryData();
                    LoadLocationInventoryData();
                    LoadTransactions();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening transfer window: {ex.Message}", "Error");
            }
        }

        private void btnSetMinimumStock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a new database context for the SetMinimumStockWindow
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var setMinimumStockWindow = new SetMinimumStockWindow(dbContext);
                var result = setMinimumStockWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
                
                if (result == true)
                {
                    // Refresh the data to show updated minimum stock values
                    LoadInventoryData();
                    LoadLocationInventoryData();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in InventoryWindow.btnSetMinimumStock_Click(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

        private void cmbLocationFilter2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyLocationInventoryFilter();
        }

        private void cmbFilter2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyLocationInventoryFilter();
        }

        private void txtSearch2_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyLocationInventoryFilter();
        }

        private void ApplyLocationInventoryFilter()
        {
            if (_allLocationInventoryItems == null) return;

            var searchText = txtSearch2.Text.ToLower();
            var filterText = (cmbFilter2.SelectedItem as ComboBoxItem)?.Content.ToString();
            var locationFilter = (cmbLocationFilter2.SelectedItem as ComboBoxItem)?.Content.ToString();

            var filteredItems = _allLocationInventoryItems.Where(item =>
            {
                // Search filter
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                  item.Name.ToLower().Contains(searchText) ||
                                  item.SKU.ToLower().Contains(searchText) ||
                                  item.Description.ToLower().Contains(searchText) ||
                                  item.LocationName.ToLower().Contains(searchText);

                // Type filter
                var matchesFilter = filterText switch
                {
                    "Low Stock" => item.StockStatus == "Low Stock",
                    "Out of Stock" => item.StockStatus == "Out of Stock",
                    "Products Only" => item.ItemType == "Product",
                    "Components Only" => item.ItemType == "Component",
                    _ => true // "All Items"
                };

                // Location filter
                var matchesLocation = locationFilter == "All Locations" || locationFilter == null || 
                                    item.LocationName == locationFilter;

                return matchesSearch && matchesFilter && matchesLocation;
            }).ToList();

            dgLocationInventory.ItemsSource = filteredItems;
        }

        private void dgLocationInventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedLocationInventoryItem = dgLocationInventory.SelectedItem as LocationInventoryItem;
        }

        #endregion

        #region Transactions Tab

        private void LoadTransactions()
        {
            try
            {
                _allTransactions = GetAllTransactions();
                dgTransactions.ItemsSource = _allTransactions;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading transactions:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Transaction Error");
            }
        }

        private List<InventoryTransaction> GetAllTransactions()
        {
            var transactions = new List<InventoryTransaction>();
            
            using var connection = _databaseContext.GetConnection();
            var sql = @"
                SELECT it.Id, it.TransactionDate, it.TransactionType, it.LocationId, it.ItemId, it.ItemType, 
                       it.Quantity, it.Notes, l.Name as LocationName,
                       CASE 
                           WHEN it.ItemType = 'Product' THEN p.Name
                           WHEN it.ItemType = 'Component' THEN c.Name
                           ELSE 'Unknown Item'
                       END as ItemName
                FROM InventoryTransactions it
                LEFT JOIN Locations l ON it.LocationId = l.Id
                LEFT JOIN Products p ON it.ItemId = p.Id AND it.ItemType = 'Product'
                LEFT JOIN Components c ON it.ItemId = c.Id AND it.ItemType = 'Component'
                ORDER BY it.TransactionDate DESC";
            
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                transactions.Add(new InventoryTransaction
                {
                    Id = reader.GetInt32(0),
                    TransactionDate = reader.GetDateTime(1),
                    TransactionType = (TransactionType)Enum.Parse(typeof(TransactionType), reader.GetString(2)),
                    LocationId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                    ItemId = reader.GetInt32(4),
                    ItemType = reader.GetString(5),
                    Quantity = reader.GetInt32(6),
                    Notes = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    LocationName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    ItemName = reader.IsDBNull(9) ? "" : reader.GetString(9)
                });
            }
            
            return transactions;
        }

        private void btnRefreshTransactions_Click(object sender, RoutedEventArgs e)
        {
            LoadTransactions();
        }

        private void btnFilterTransactions_Click(object sender, RoutedEventArgs e)
        {
            FilterTransactions();
        }

        private void FilterTransactions()
        {
            if (_allTransactions == null) return;

            var startDate = dpStartDate.SelectedDate ?? DateTime.Today.AddDays(-30);
            var endDate = dpEndDate.SelectedDate ?? DateTime.Today;

            var filteredTransactions = _allTransactions.Where(t => 
                t.TransactionDate.Date >= startDate.Date && 
                t.TransactionDate.Date <= endDate.Date).ToList();

            dgTransactions.ItemsSource = filteredTransactions;
        }

        #endregion

        #region Alerts Tab

        private void LoadAlerts()
        {
            try
            {
                _allAlerts = GetAllAlerts();
                dgAlerts.ItemsSource = _allAlerts;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading alerts:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Alert Error");
            }
        }

        private List<InventoryAlert> GetAllAlerts()
        {
            var alerts = new List<InventoryAlert>();
            
            // Generate alerts based on current inventory status
            if (_allInventoryItems != null)
            {
                foreach (var item in _allInventoryItems)
                {
                    if (item.CurrentStock <= 0)
                    {
                        alerts.Add(new InventoryAlert
                        {
                            AlertDate = DateTime.Now,
                            AlertType = "Out of Stock",
                            ItemName = item.Name,
                            LocationName = "All Locations",
                            CurrentStock = item.CurrentStock,
                            MinimumStock = item.MinimumStock,
                            Message = $"{item.ItemType} '{item.Name}' is out of stock"
                        });
                    }
                    else if (item.CurrentStock <= item.MinimumStock)
                    {
                        alerts.Add(new InventoryAlert
                        {
                            AlertDate = DateTime.Now,
                            AlertType = "Low Stock",
                            ItemName = item.Name,
                            LocationName = "All Locations",
                            CurrentStock = item.CurrentStock,
                            MinimumStock = item.MinimumStock,
                            Message = $"{item.ItemType} '{item.Name}' is running low on stock"
                        });
                    }
                }
            }
            
            return alerts;
        }

        private void btnRefreshAlerts_Click(object sender, RoutedEventArgs e)
        {
            LoadAlerts();
        }

        private void btnDismissAlert_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAlert != null)
            {
                var result = ErrorDialog.ShowConfirmation("Are you sure you want to dismiss this alert?", "Confirm Dismiss");
                
                if (result)
                {
                    _allAlerts.Remove(_selectedAlert);
                    dgAlerts.ItemsSource = null;
                    dgAlerts.ItemsSource = _allAlerts;
                    _selectedAlert = null;
                }
            }
            else
            {
                ErrorDialog.ShowWarning("Please select an alert to dismiss.", "No Selection");
            }
        }

        private void cmbAlertType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyAlertFilter();
        }

        private void ApplyAlertFilter()
        {
            if (_allAlerts == null) return;

            var filterText = (cmbAlertType.SelectedItem as ComboBoxItem)?.Content.ToString();

            var filteredAlerts = _allAlerts.Where(alert =>
            {
                return filterText switch
                {
                    "Low Stock" => alert.AlertType == "Low Stock",
                    "Out of Stock" => alert.AlertType == "Out of Stock",
                    "Expiring Soon" => alert.AlertType == "Expiring Soon",
                    _ => true // "All Alerts"
                };
            }).ToList();

            dgAlerts.ItemsSource = filteredAlerts;
        }

        private void dgAlerts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedAlert = dgAlerts.SelectedItem as InventoryAlert;
        }

        #endregion
    }
} 