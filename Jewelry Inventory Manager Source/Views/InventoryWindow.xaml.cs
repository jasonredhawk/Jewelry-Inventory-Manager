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
        private List<Location> _allLocations;
        private List<Category> _allCategories;
        private LocationInventoryItem _selectedInventoryItem;

        public InventoryWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            InitializeDatePickers();
            LoadLocations();
            LoadCategories();
            LoadInventoryData();
            LoadTransactions();
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

        private void LoadCategories()
        {
            try
            {
                _allCategories = _databaseContext.GetAllCategories();
                
                // Populate category filter dropdown
                cmbCategoryFilter.Items.Clear();
                cmbCategoryFilter.Items.Add(new ComboBoxItem { Content = "All Categories", IsSelected = true });
                
                foreach (var category in _allCategories.Where(c => c.IsActive))
                {
                    cmbCategoryFilter.Items.Add(new ComboBoxItem { Content = category.Name, Tag = category.Id });
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading categories:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Category Error");
            }
        }

        #region Stock Overview Tab

        private void LoadInventoryData()
        {
            try
            {
                _allInventoryItems = GetAllInventoryItems();
                
                // Ensure all items have their stock status calculated
                foreach (var item in _allInventoryItems)
                {
                    item.UpdateStockStatus();
                }
                
                dgInventory.ItemsSource = _allInventoryItems;
                UpdateSummaryCardsFromFilteredData(_allInventoryItems);
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
                    var fullStock = _databaseContext.GetFullStock("Product", product.Id, location.Id);
                    
                    var item = new LocationInventoryItem
                    {
                        Id = product.Id,
                        SKU = product.SKU,
                        Name = product.Name,
                        Description = product.Description ?? "",
                        ItemType = "Product",
                        LocationId = location.Id,
                        LocationName = location.Name,
                        Price = product.Price,
                        TotalStockAcrossLocations = totalStock,
                        IsActive = product.IsActive,
                        LastModified = product.LastModified,
                        CategoryId = product.CategoryId
                    };
                    
                    // Set stock-related properties in the correct order to avoid multiple recalculations
                    item.MinimumStock = minStock;
                    item.FullStock = fullStock;
                    item.CurrentStock = productStock; // This will trigger the final UpdateStockStatus()
                    
                    items.Add(item);
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
                    var fullStock = _databaseContext.GetFullStock("Component", component.Id, location.Id);
                    
                    var item = new LocationInventoryItem
                    {
                        Id = component.Id,
                        SKU = component.SKU,
                        Name = component.Name,
                        Description = component.Description ?? "",
                        ItemType = "Component",
                        LocationId = location.Id,
                        LocationName = location.Name,
                        Cost = component.Cost,
                        TotalStockAcrossLocations = totalStock,
                        IsActive = component.IsActive,
                        LastModified = component.LastModified,
                        CategoryId = component.CategoryId
                    };
                    
                    // Set stock-related properties in the correct order to avoid multiple recalculations
                    item.MinimumStock = minStock;
                    item.FullStock = fullStock;
                    item.CurrentStock = componentStock; // This will trigger the final UpdateStockStatus()
                    
                    items.Add(item);
                }
            }
            
            return items;
        }

        private void UpdateSummaryCards()
        {
            if (_allInventoryItems == null) return;

            // Count unique items (by ID) for Products and Components
            var uniqueItems = _allInventoryItems.GroupBy(x => x.Id).Select(g => g.First()).ToList();
            var totalProducts = uniqueItems.Count(x => x.ItemType == "Product");
            var totalComponents = uniqueItems.Count(x => x.ItemType == "Component");
            
            // Count actual items for stock status (including duplicates from multiple locations)
            var inStockItems = _allInventoryItems.Count(x => x.StockStatus == "In Stock");
            var lowStockItems = _allInventoryItems.Count(x => x.StockStatus == "Low Stock");
            var outOfStockItems = _allInventoryItems.Count(x => x.StockStatus == "Out of Stock");
            var overStockItems = _allInventoryItems.Count(x => x.StockStatus == "Over Stock");

            txtTotalProducts.Text = totalProducts.ToString();
            txtTotalComponents.Text = totalComponents.ToString();
            txtInStockItems.Text = inStockItems.ToString();
            txtLowStockItems.Text = lowStockItems.ToString();
            txtOutOfStock.Text = outOfStockItems.ToString();
            txtOverStockItems.Text = overStockItems.ToString();
        }

        private void UpdateSummaryCardsFromFilteredData(List<LocationInventoryItem> filteredItems)
        {
            if (filteredItems == null) return;

            // Count unique items (by SKU) for Products and Components
            var uniqueItems = filteredItems.GroupBy(x => x.SKU).Select(g => g.First()).ToList();
            var totalProducts = uniqueItems.Count(x => x.ItemType == "Product");
            var totalComponents = uniqueItems.Count(x => x.ItemType == "Component");
            
            // Debug: Show component counting details
            System.Diagnostics.Debug.WriteLine($"=== Summary Cards Debug ===");
            System.Diagnostics.Debug.WriteLine($"Total filtered items: {filteredItems.Count}");
            System.Diagnostics.Debug.WriteLine($"Unique items by SKU: {uniqueItems.Count}");
            System.Diagnostics.Debug.WriteLine($"Components in filtered items: {filteredItems.Count(x => x.ItemType == "Component")}");
            System.Diagnostics.Debug.WriteLine($"Unique components by SKU: {totalComponents}");
            System.Diagnostics.Debug.WriteLine($"Components by SKU: {string.Join(", ", filteredItems.Where(x => x.ItemType == "Component").Select(x => $"{x.Name}(SKU:{x.SKU})"))}");
            
            // Count actual items for stock status (including duplicates from multiple locations)
            var inStockItems = filteredItems.Count(x => x.StockStatus == "In Stock");
            var lowStockItems = filteredItems.Count(x => x.StockStatus == "Low Stock");
            var outOfStockItems = filteredItems.Count(x => x.StockStatus == "Out of Stock");
            var overStockItems = filteredItems.Count(x => x.StockStatus == "Over Stock");

            txtTotalProducts.Text = totalProducts.ToString();
            txtTotalComponents.Text = totalComponents.ToString();
            txtInStockItems.Text = inStockItems.ToString();
            txtLowStockItems.Text = lowStockItems.ToString();
            txtOutOfStock.Text = outOfStockItems.ToString();
            txtOverStockItems.Text = overStockItems.ToString();
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

        private void cmbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyInventoryFilter();
        }

        private void cmbItemTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            var categoryFilter = (cmbCategoryFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            var selectedCategoryId = (cmbCategoryFilter.SelectedItem as ComboBoxItem)?.Tag as int?;
            var itemTypeFilter = (cmbItemTypeFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Debug: Check what filter is selected
            System.Diagnostics.Debug.WriteLine($"Selected filter: '{filterText}'");
            
            var filteredItems = _allInventoryItems.Where(item =>
            {
                // Search filter
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                  item.Name.ToLower().Contains(searchText) ||
                                  item.SKU.ToLower().Contains(searchText) ||
                                  item.Description.ToLower().Contains(searchText);

                // Stock Levels filter
                var matchesStockFilter = filterText switch
                {
                    "In Stock" => item.StockStatus == "In Stock",
                    "Low Stock" => item.StockStatus == "Low Stock",
                    "Out of Stock" => item.StockStatus == "Out of Stock",
                    "Over Stock" => item.StockStatus == "Over Stock",
                    _ => true // "All Items"
                };

                // Location filter
                var matchesLocation = locationFilter == "All Locations" || locationFilter == null || 
                                    (selectedLocationId.HasValue && item.LocationId == selectedLocationId.Value);

                // Category filter
                var matchesCategory = categoryFilter == "All Categories" || categoryFilter == null ||
                                    (selectedCategoryId.HasValue && item.CategoryId == selectedCategoryId.Value);

                // Item Type filter
                var matchesItemType = itemTypeFilter switch
                {
                    "Products" => item.ItemType == "Product",
                    "Components" => item.ItemType == "Component",
                    _ => true // "All Types"
                };

                // Debug: Show all items with their status when filtering for stock levels
                if (filterText == "In Stock" || filterText == "Low Stock" || filterText == "Out of Stock" || filterText == "Over Stock")
                {
                    System.Diagnostics.Debug.WriteLine($"Item: {item.Name}, Status: '{item.StockStatus}', CurrentStock: {item.CurrentStock}, MinStock: {item.MinimumStock}, FullStock: {item.FullStock}, MatchesStockFilter: {matchesStockFilter}");
                }

                return matchesSearch && matchesStockFilter && matchesLocation && matchesCategory && matchesItemType;
            }).ToList();
            
            System.Diagnostics.Debug.WriteLine($"Total items: {_allInventoryItems.Count}, Filtered items: {filteredItems.Count}");
            dgInventory.ItemsSource = filteredItems;
            
            // Update summary cards based on filtered data
            UpdateSummaryCardsFromFilteredData(filteredItems);
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
    }
} 