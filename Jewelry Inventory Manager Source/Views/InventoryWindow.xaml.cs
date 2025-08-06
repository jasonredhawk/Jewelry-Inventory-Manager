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
        private List<LocationInventoryItem> _allInventoryItems;
        private List<InventoryTransaction> _allTransactions;
        private List<InventoryAlert> _allAlerts;
        private List<Location> _allLocations;
        private LocationInventoryItem _selectedInventoryItem;
        private InventoryAlert _selectedAlert;

        public InventoryWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            InitializeDatePickers();
            LoadLocations();
            LoadInventoryData();
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
                
                foreach (var location in _allLocations)
                {
                    cmbLocationFilter.Items.Add(new ComboBoxItem { Content = location.Name, Tag = location.Id });
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

        private List<LocationInventoryItem> GetAllInventoryItems()
        {
            var items = new List<LocationInventoryItem>();
            var locations = _databaseContext.GetAllLocations();
            
            // Get all products and calculate their stock for each location
            var products = _databaseContext.GetAllProducts();
            foreach (var product in products.Where(p => p.IsActive))
            {
                var totalStock = 0;
                var totalMinStock = 0;
                
                // Calculate total stock across all locations
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    var productStock = _databaseContext.GetProductStock(product.Id, location.Id);
                    var minStock = _databaseContext.GetMinimumStock("Product", product.Id, location.Id);
                    
                    totalStock += productStock;
                    totalMinStock += minStock;
                }
                
                // Create an item for each location
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    var productStock = _databaseContext.GetProductStock(product.Id, location.Id);
                    var minStock = _databaseContext.GetMinimumStock("Product", product.Id, location.Id);
                    
                    items.Add(new LocationInventoryItem
                    {
                        Id = product.Id,
                        SKU = product.SKU,
                        Name = product.Name,
                        Description = product.Description ?? "",
                        ItemType = "Product",
                        LocationId = location.Id,
                        LocationName = location.Name,
                        Price = product.Price,
                        CurrentStock = productStock,
                        TotalStockAcrossLocations = totalStock,
                        MinimumStock = minStock,
                        IsActive = product.IsActive,
                        LastModified = product.LastModified
                    });
                }
            }
            
            // Get all components and calculate their stock for each location
            var components = _databaseContext.GetAllComponents();
            foreach (var component in components.Where(c => c.IsActive))
            {
                var totalStock = 0;
                var totalMinStock = 0;
                
                // Calculate total stock across all locations
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    var componentStock = _databaseContext.GetComponentStock(component.Id, location.Id);
                    var minStock = _databaseContext.GetMinimumStock("Component", component.Id, location.Id);
                    
                    totalStock += componentStock;
                    totalMinStock += minStock;
                }
                
                // Create an item for each location
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    var componentStock = _databaseContext.GetComponentStock(component.Id, location.Id);
                    var minStock = _databaseContext.GetMinimumStock("Component", component.Id, location.Id);
                    
                    items.Add(new LocationInventoryItem
                    {
                        Id = component.Id,
                        SKU = component.SKU,
                        Name = component.Name,
                        Description = component.Description ?? "",
                        ItemType = "Component",
                        LocationId = location.Id,
                        LocationName = location.Name,
                        Cost = component.Cost,
                        CurrentStock = componentStock,
                        TotalStockAcrossLocations = totalStock,
                        MinimumStock = minStock,
                        IsActive = component.IsActive,
                        LastModified = component.LastModified
                    });
                }
            }
            
            return items;
        }

        private void UpdateSummaryCards()
        {
            if (_allInventoryItems == null) return;

            // Get unique items (by ID) for summary calculations
            var uniqueItems = _allInventoryItems.GroupBy(x => x.Id).Select(g => g.First()).ToList();
            
            var totalProducts = uniqueItems.Count(x => x.ItemType == "Product");
            var totalComponents = uniqueItems.Count(x => x.ItemType == "Component");
            var lowStockItems = uniqueItems.Count(x => x.StockStatus == "Low Stock");
            var outOfStockItems = uniqueItems.Count(x => x.StockStatus == "Out of Stock");

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
                // Create a new database context for the AddInventoryTransactionWindow
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var addTransactionWindow = new AddInventoryTransactionWindow(dbContext);
                var result = addTransactionWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
                
                if (result == true)
                {
                    // Refresh the data to show updated stock values
                    LoadInventoryData();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in InventoryWindow.btnAddTransaction_Click(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a new database context for the InventoryTransferWindow
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var transferWindow = new InventoryTransferWindow(dbContext);
                var result = transferWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
                
                if (result == true)
                {
                    // Refresh the data to show updated stock values
                    LoadInventoryData();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in InventoryWindow.btnTransfer_Click(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

        private void btnGeneratePO_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a new database context for the GeneratePOWindow
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var generatePOWindow = new GeneratePOWindow(dbContext);
                var result = generatePOWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
                
                if (result == true)
                {
                    // Refresh the data to show updated stock values
                    LoadInventoryData();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in InventoryWindow.btnGeneratePO_Click(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
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
            var selectedLocationId = (cmbLocationFilter.SelectedItem as ComboBoxItem)?.Tag as int?;

            // Show/hide location-specific columns based on filter
            if (locationFilter == "All Locations" || locationFilter == null)
            {
                colLocation.Visibility = Visibility.Collapsed;
                dgInventory.Columns[4].Visibility = Visibility.Collapsed; // Location Stock column
            }
            else
            {
                colLocation.Visibility = Visibility.Visible;
                dgInventory.Columns[4].Visibility = Visibility.Visible; // Location Stock column
            }

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

                // Location filter
                var matchesLocation = locationFilter == "All Locations" || locationFilter == null || 
                                    (selectedLocationId.HasValue && item.LocationId == selectedLocationId.Value);

                return matchesSearch && matchesFilter && matchesLocation;
            }).ToList();

            dgInventory.ItemsSource = filteredItems;
        }

        private void dgInventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedInventoryItem = dgInventory.SelectedItem as LocationInventoryItem;
        }

        private void btnSetStockLevels_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a new database context for the SetStockLevelsWindow
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var setStockLevelsWindow = new SetStockLevelsWindow(dbContext);
                var result = setStockLevelsWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
                
                if (result == true)
                {
                    // Refresh the data to show updated stock values
                    LoadInventoryData();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in InventoryWindow.btnSetStockLevels_Click(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
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
                            LocationName = item.LocationName,
                            CurrentStock = item.CurrentStock,
                            MinimumStock = item.MinimumStock,
                            Message = $"{item.ItemType} '{item.Name}' is out of stock at {item.LocationName}"
                        });
                    }
                    else if (item.CurrentStock <= item.MinimumStock)
                    {
                        alerts.Add(new InventoryAlert
                        {
                            AlertDate = DateTime.Now,
                            AlertType = "Low Stock",
                            ItemName = item.Name,
                            LocationName = item.LocationName,
                            CurrentStock = item.CurrentStock,
                            MinimumStock = item.MinimumStock,
                            Message = $"{item.ItemType} '{item.Name}' is running low on stock at {item.LocationName}"
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