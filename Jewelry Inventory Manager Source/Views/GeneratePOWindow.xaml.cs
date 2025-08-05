using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class GeneratePOWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<LocationInventoryItem> _allItems;
        private List<LocationInventoryItem> _filteredItems;
        private List<LocationInventoryItem> _selectedItems;
        private DispatcherTimer _updateTimer;

        public GeneratePOWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _selectedItems = new List<LocationInventoryItem>();
            
            // Initialize update timer
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(100);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
            
            LoadLocations();
            LoadData();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateSelectedItems();
            UpdateSummaryCards();
        }

        private void LoadLocations()
        {
            try
            {
                var locations = _databaseContext.GetAllLocations();
                cmbLocationFilter.Items.Clear();
                cmbLocationFilter.Items.Add(new ComboBoxItem { Content = "All Locations", IsSelected = true });
                
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    cmbLocationFilter.Items.Add(new ComboBoxItem { Content = location.Name, Tag = location.Id });
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading locations: {ex.Message}", "Error");
            }
        }

        private void LoadData()
        {
            try
            {
                _allItems = GetAllLowStockItems();
                _filteredItems = _allItems.ToList();
                
                // Subscribe to property changes for all items
                foreach (var item in _filteredItems)
                {
                    item.PropertyChanged += InventoryItem_PropertyChanged;
                }
                
                dgItems.ItemsSource = _filteredItems;
                UpdateSummaryCards();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void InventoryItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocationInventoryItem.IsSelected))
            {
                // Use BeginInvoke to avoid conflicts with edit transactions
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateSelectedItems();
                    UpdateSummaryCards();
                    // Debug: Force UI update
                    txtSelectedCount.GetBindingExpression(TextBlock.TextProperty)?.UpdateTarget();
                }));
            }
        }

        private List<LocationInventoryItem> GetAllLowStockItems()
        {
            var items = new List<LocationInventoryItem>();
            var locations = _databaseContext.GetAllLocations();
            
            // Get all products and check their stock across all locations
            var products = _databaseContext.GetAllProducts();
            foreach (var product in products.Where(p => p.IsActive))
            {
                var totalStock = 0;
                var totalMinStock = 0;
                var hasLowStock = false;
                
                // Calculate total stock across all locations
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    var productStock = _databaseContext.GetProductStock(product.Id, location.Id);
                    var minStock = _databaseContext.GetMinimumStock("Product", product.Id, location.Id);
                    
                    totalStock += productStock;
                    totalMinStock += minStock;
                    
                    // Check if this location has low stock
                    if (productStock <= minStock || productStock == 0)
                    {
                        hasLowStock = true;
                    }
                }
                
                // Only add products that have low stock in at least one location
                if (hasLowStock)
                {
                    // Create an item for each location that has low stock
                    foreach (var location in locations.Where(l => l.IsActive))
                    {
                        var productStock = _databaseContext.GetProductStock(product.Id, location.Id);
                        var minStock = _databaseContext.GetMinimumStock("Product", product.Id, location.Id);
                        
                        // Only add if this location has low stock
                        if (productStock <= minStock || productStock == 0)
                        {
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
                                LastModified = product.LastModified,
                                IsSelected = false
                            });
                        }
                    }
                }
            }
            
            // Get all components and check their stock across all locations
            var components = _databaseContext.GetAllComponents();
            foreach (var component in components.Where(c => c.IsActive))
            {
                var totalStock = 0;
                var totalMinStock = 0;
                var hasLowStock = false;
                
                // Calculate total stock across all locations
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    var componentStock = _databaseContext.GetComponentStock(component.Id, location.Id);
                    var minStock = _databaseContext.GetMinimumStock("Component", component.Id, location.Id);
                    
                    totalStock += componentStock;
                    totalMinStock += minStock;
                    
                    // Check if this location has low stock
                    if (componentStock <= minStock || componentStock == 0)
                    {
                        hasLowStock = true;
                    }
                }
                
                // Only add components that have low stock in at least one location
                if (hasLowStock)
                {
                    // Create an item for each location that has low stock
                    foreach (var location in locations.Where(l => l.IsActive))
                    {
                        var componentStock = _databaseContext.GetComponentStock(component.Id, location.Id);
                        var minStock = _databaseContext.GetMinimumStock("Component", component.Id, location.Id);
                        
                        // Only add if this location has low stock
                        if (componentStock <= minStock || componentStock == 0)
                        {
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
                                LastModified = component.LastModified,
                                IsSelected = false
                            });
                        }
                    }
                }
            }
            
            return items.OrderBy(i => i.Name).ToList();
        }

        private void UpdateSummaryCards()
        {
            if (_allItems == null)
            {
                txtLowStockCount.Text = "0";
                txtOutOfStockCount.Text = "0";
                txtTotalValue.Text = "$0.00";
                txtSelectedCount.Text = "0";
                return;
            }

            var lowStockCount = _allItems.Count(i => i.CurrentStock > 0 && i.CurrentStock <= i.MinimumStock);
            var outOfStockCount = _allItems.Count(i => i.CurrentStock <= 0);
            
            // Calculate total value based on selected items only
            var totalValue = _selectedItems.Sum(i => 
            {
                var quantityToOrder = Math.Max(i.MinimumStock - i.CurrentStock, 1);
                
                if (i.ItemType == "Product")
                {
                    // For products, calculate based on their components
                    var productComponents = _databaseContext.GetProductComponents(i.Id);
                    return productComponents.Sum(c => 
                    {
                        var componentQuantity = quantityToOrder * c.Quantity;
                        var componentItem = _allItems.FirstOrDefault(item => 
                            item.ItemType == "Component" && 
                            item.Id == c.ComponentId && 
                            item.LocationId == i.LocationId);
                        return componentItem?.Cost * componentQuantity ?? 0;
                    });
                }
                else
                {
                    // For components, use their cost
                    return quantityToOrder * i.Cost;
                }
            });
            
            var selectedCount = _selectedItems.Count;

            txtLowStockCount.Text = lowStockCount.ToString();
            txtOutOfStockCount.Text = outOfStockCount.ToString();
            txtTotalValue.Text = totalValue.ToString("C");
            txtSelectedCount.Text = selectedCount.ToString();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void cmbLocationFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allItems == null)
                return;

            var searchText = txtSearch.Text.ToLower();
            var filterType = (cmbFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            var locationFilter = (cmbLocationFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            var selectedLocationId = (cmbLocationFilter.SelectedItem as ComboBoxItem)?.Tag as int?;

            _filteredItems = _allItems.Where(item =>
            {
                var matchesSearch = string.IsNullOrWhiteSpace(searchText) ||
                                  item.Name.ToLower().Contains(searchText) ||
                                  item.SKU.ToLower().Contains(searchText) ||
                                  item.Description.ToLower().Contains(searchText);

                var matchesFilter = filterType switch
                {
                    "Low & Out of Stock" or "Low &amp; Out of Stock" => (item.CurrentStock <= item.MinimumStock || item.CurrentStock == 0),
                    "Low Stock Only" => item.CurrentStock > 0 && item.CurrentStock <= item.MinimumStock,
                    "Out of Stock Only" => item.CurrentStock <= 0,
                    "Products Only" => item.ItemType == "Product",
                    "Components Only" => item.ItemType == "Component",
                    _ => true
                };

                var matchesLocation = locationFilter == "All Locations" || locationFilter == null || 
                                    (selectedLocationId.HasValue && item.LocationId == selectedLocationId.Value);

                return matchesSearch && matchesFilter && matchesLocation;
            }).ToList();

            // Subscribe to property changes for filtered items
            foreach (var item in _filteredItems)
            {
                item.PropertyChanged += InventoryItem_PropertyChanged;
            }

            dgItems.ItemsSource = _filteredItems;
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _filteredItems)
            {
                item.IsSelected = true;
            }
            UpdateSelectedItems();
            UpdateSummaryCards();
        }

        private void btnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _filteredItems)
            {
                item.IsSelected = false;
            }
            UpdateSelectedItems();
            UpdateSummaryCards();
        }

        private void UpdateSelectedItems()
        {
            _selectedItems = _filteredItems.Where(item => item.IsSelected).ToList();
        }

        private void dgItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedItems();
            UpdateSummaryCards();
        }

        private void dgItems_CurrentCellChanged(object sender, EventArgs e)
        {
            // This event fires when the current cell changes, including checkbox clicks
            UpdateSelectedItems();
            UpdateSummaryCards();
        }



        private void btnGeneratePO_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItems.Count == 0)
            {
                ErrorDialog.ShowWarning("Please select items for the purchase order.", "No Items Selected");
                return;
            }

            try
            {
                GeneratePurchaseOrder();
                ErrorDialog.ShowSuccess("Purchase order generated successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error generating purchase order: {ex.Message}", "Error");
            }
        }

        private void GeneratePurchaseOrder()
        {
            var poNumber = $"PO-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";
            var poDate = DateTime.Now;
            var fileName = $"PurchaseOrder_{poNumber}_{poDate:yyyyMMdd}.csv";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            // Write header
            csv.WriteField("Purchase Order Number");
            csv.WriteField("Date");
            csv.WriteField("Location");
            csv.WriteField("Item Type");
            csv.WriteField("SKU");
            csv.WriteField("Name");
            csv.WriteField("Description");
            csv.WriteField("Location Stock");
            csv.WriteField("Total Stock");
            csv.WriteField("Minimum Stock");
            csv.WriteField("Quantity to Order");
            csv.WriteField("Unit Price/Cost");
            csv.WriteField("Total Line Value");
            csv.WriteField("Notes");
            csv.NextRecord();

            var totalValue = 0m;
            var processedItems = new List<string>(); // Track processed items to avoid duplicates

            foreach (var item in _selectedItems)
            {
                if (item.ItemType == "Product")
                {
                    // For products, add their components to the PO
                    var productComponents = _databaseContext.GetProductComponents(item.Id);
                    var quantityToOrder = Math.Max(item.MinimumStock - item.CurrentStock, 1);
                    
                    foreach (var component in productComponents)
                    {
                        var componentItem = _allItems.FirstOrDefault(i => 
                            i.ItemType == "Component" && 
                            i.Id == component.ComponentId && 
                            i.LocationId == item.LocationId);
                        
                        if (componentItem != null)
                        {
                            var componentQuantityToOrder = quantityToOrder * component.Quantity;
                            var componentUnitPrice = componentItem.Cost;
                            var componentLineValue = componentQuantityToOrder * componentUnitPrice;
                            totalValue += componentLineValue;

                            csv.WriteField(poNumber);
                            csv.WriteField(poDate.ToString("yyyy-MM-dd"));
                            csv.WriteField(item.LocationName);
                            csv.WriteField("Component");
                            csv.WriteField(componentItem.SKU);
                            csv.WriteField(componentItem.Name);
                            csv.WriteField(componentItem.Description);
                            csv.WriteField(componentItem.CurrentStock);
                            csv.WriteField(componentItem.TotalStockAcrossLocations);
                            csv.WriteField(componentItem.MinimumStock);
                            csv.WriteField(componentQuantityToOrder);
                            csv.WriteField(componentUnitPrice.ToString("C"));
                            csv.WriteField(componentLineValue.ToString("C"));
                            csv.WriteField($"For Product: {item.Name} (Qty: {quantityToOrder})");
                            csv.NextRecord();
                        }
                    }
                }
                else
                {
                    // For components, add directly to PO
                    var quantityToOrder = Math.Max(item.MinimumStock - item.CurrentStock, 1);
                    var unitPrice = item.Cost;
                    var lineValue = quantityToOrder * unitPrice;
                    totalValue += lineValue;

                    csv.WriteField(poNumber);
                    csv.WriteField(poDate.ToString("yyyy-MM-dd"));
                    csv.WriteField(item.LocationName);
                    csv.WriteField(item.ItemType);
                    csv.WriteField(item.SKU);
                    csv.WriteField(item.Name);
                    csv.WriteField(item.Description);
                    csv.WriteField(item.CurrentStock);
                    csv.WriteField(item.TotalStockAcrossLocations);
                    csv.WriteField(item.MinimumStock);
                    csv.WriteField(quantityToOrder);
                    csv.WriteField(unitPrice.ToString("C"));
                    csv.WriteField(lineValue.ToString("C"));
                    csv.WriteField("");
                    csv.NextRecord();
                }
            }

            // Write summary
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("TOTAL:");
            csv.WriteField(totalValue.ToString("C"));
            csv.NextRecord();

            // Save PO record to database
            SavePurchaseOrderRecord(poNumber, poDate, totalValue);

            ErrorDialog.ShowSuccess($"Purchase order saved to: {filePath}\nTotal Value: {totalValue:C}", "Purchase Order Generated");
        }

        private void SavePurchaseOrderRecord(string poNumber, DateTime poDate, decimal totalValue)
        {
            using var connection = _databaseContext.GetConnection();
            var sql = @"
                INSERT INTO PurchaseOrders 
                (PONumber, PODate, TotalValue, Status, CreatedDate) 
                VALUES (@poNumber, @poDate, @totalValue, 'Pending', @createdDate)";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@poNumber", poNumber);
            command.Parameters.AddWithValue("@poDate", poDate);
            command.Parameters.AddWithValue("@totalValue", totalValue);
            command.Parameters.AddWithValue("@createdDate", DateTime.Now);

            command.ExecuteNonQuery();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _updateTimer?.Stop();
            base.OnClosed(e);
        }
    }
} 