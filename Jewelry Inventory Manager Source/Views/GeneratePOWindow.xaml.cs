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
        private List<InventoryItem> _allItems;
        private List<InventoryItem> _filteredItems;
        private List<InventoryItem> _selectedItems;
        private DispatcherTimer _updateTimer;

        public GeneratePOWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _selectedItems = new List<InventoryItem>();
            
            // Initialize update timer
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(100);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
            
            LoadData();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateSelectedItems();
            UpdateSummaryCards();
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
            if (e.PropertyName == nameof(InventoryItem.IsSelected))
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

        private List<InventoryItem> GetAllLowStockItems()
        {
            var items = new List<InventoryItem>();
            
            using var connection = _databaseContext.GetConnection();
            
            // Get Products with low stock
            var productSql = @"
                SELECT Id, SKU, Name, Description, Price, CurrentStock, MinimumStock, IsActive, LastModified 
                FROM Products 
                WHERE IsActive = 1 AND (CurrentStock <= MinimumStock OR CurrentStock = 0)
                ORDER BY Name";
            using var productCommand = new MySqlCommand(productSql, connection);
            using var productReader = productCommand.ExecuteReader();
            
            while (productReader.Read())
            {
                items.Add(new InventoryItem
                {
                    Id = productReader.GetInt32(0),
                    SKU = productReader.GetString(1),
                    Name = productReader.GetString(2),
                    Description = productReader.IsDBNull(3) ? "" : productReader.GetString(3),
                    ItemType = "Product",
                    Price = productReader.GetDecimal(4),
                    CurrentStock = productReader.GetInt32(5),
                    MinimumStock = productReader.GetInt32(6),
                    IsActive = productReader.GetBoolean(7),
                    LastModified = productReader.GetDateTime(8),
                    IsSelected = false
                });
            }
            
            productReader.Close();
            
            // Get Components with low stock
            var componentSql = @"
                SELECT Id, SKU, Name, Description, Cost, CurrentStock, MinimumStock, IsActive, LastModified 
                FROM Components 
                WHERE IsActive = 1 AND (CurrentStock <= MinimumStock OR CurrentStock = 0)
                ORDER BY Name";
            using var componentCommand = new MySqlCommand(componentSql, connection);
            using var componentReader = componentCommand.ExecuteReader();
            
            while (componentReader.Read())
            {
                items.Add(new InventoryItem
                {
                    Id = componentReader.GetInt32(0),
                    SKU = componentReader.GetString(1),
                    Name = componentReader.GetString(2),
                    Description = componentReader.IsDBNull(3) ? "" : componentReader.GetString(3),
                    ItemType = "Component",
                    Cost = componentReader.GetDecimal(4),
                    CurrentStock = componentReader.GetInt32(5),
                    MinimumStock = componentReader.GetInt32(6),
                    IsActive = componentReader.GetBoolean(7),
                    LastModified = componentReader.GetDateTime(8),
                    IsSelected = false
                });
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
                var unitPrice = i.ItemType == "Product" ? i.Price : i.Cost;
                return quantityToOrder * unitPrice;
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

        private void ApplyFilter()
        {
            if (_allItems == null)
                return;

            var searchText = txtSearch.Text.ToLower();
            var filterType = (cmbFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

            _filteredItems = _allItems.Where(item =>
            {
                var matchesSearch = string.IsNullOrWhiteSpace(searchText) ||
                                  item.Name.ToLower().Contains(searchText) ||
                                  item.SKU.ToLower().Contains(searchText) ||
                                  item.Description.ToLower().Contains(searchText);

                var matchesFilter = filterType switch
                {
                    "Low Stock Only" => item.CurrentStock > 0 && item.CurrentStock <= item.MinimumStock,
                    "Out of Stock Only" => item.CurrentStock <= 0,
                    "Products Only" => item.ItemType == "Product",
                    "Components Only" => item.ItemType == "Component",
                    _ => true
                };

                return matchesSearch && matchesFilter;
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
            csv.WriteField("Item Type");
            csv.WriteField("SKU");
            csv.WriteField("Name");
            csv.WriteField("Description");
            csv.WriteField("Current Stock");
            csv.WriteField("Minimum Stock");
            csv.WriteField("Quantity to Order");
            csv.WriteField("Unit Price/Cost");
            csv.WriteField("Total Line Value");
            csv.NextRecord();

            var totalValue = 0m;

            foreach (var item in _selectedItems)
            {
                var quantityToOrder = Math.Max(item.MinimumStock - item.CurrentStock, 1);
                var unitPrice = item.ItemType == "Product" ? item.Price : item.Cost;
                var lineValue = quantityToOrder * unitPrice;
                totalValue += lineValue;

                csv.WriteField(poNumber);
                csv.WriteField(poDate.ToString("yyyy-MM-dd"));
                csv.WriteField(item.ItemType);
                csv.WriteField(item.SKU);
                csv.WriteField(item.Name);
                csv.WriteField(item.Description);
                csv.WriteField(item.CurrentStock);
                csv.WriteField(item.MinimumStock);
                csv.WriteField(quantityToOrder);
                csv.WriteField(unitPrice.ToString("C"));
                csv.WriteField(lineValue.ToString("C"));
                csv.NextRecord();
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