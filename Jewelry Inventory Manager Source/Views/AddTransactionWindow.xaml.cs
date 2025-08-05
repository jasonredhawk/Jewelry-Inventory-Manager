using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class AddTransactionWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<Product> _allProducts;
        private List<Component> _allComponents;
        private List<Location> _allLocations;
        private InventoryItem _selectedItem;

        public AddTransactionWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadData();
            InitializeTransactionTypes();
        }

        private void LoadData()
        {
            try
            {
                LoadProducts();
                LoadComponents();
                LoadLocations();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void LoadProducts()
        {
            _allProducts = new List<Product>();
            using var connection = _databaseContext.GetConnection();
            var sql = "SELECT Id, Name, SKU FROM Products WHERE IsActive = 1 ORDER BY Name";
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                _allProducts.Add(new Product
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    SKU = reader.GetString(2)
                });
            }
        }

        private void LoadComponents()
        {
            _allComponents = new List<Component>();
            using var connection = _databaseContext.GetConnection();
            var sql = "SELECT Id, Name, SKU FROM Components WHERE IsActive = 1 ORDER BY Name";
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                _allComponents.Add(new Component
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    SKU = reader.GetString(2)
                });
            }
        }

        private void LoadLocations()
        {
            _allLocations = new List<Location>();
            using var connection = _databaseContext.GetConnection();
            var sql = "SELECT Id, Name FROM Locations WHERE IsActive = 1 ORDER BY Name";
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                _allLocations.Add(new Location
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            
            cmbLocation.ItemsSource = _allLocations;
        }

        private void InitializeTransactionTypes()
        {
            var transactionTypes = Enum.GetValues(typeof(TransactionType))
                .Cast<TransactionType>()
                .Select(t => new ComboBoxItem { Content = t.ToString() })
                .ToList();
            
            cmbTransactionType.ItemsSource = transactionTypes;
            cmbTransactionType.SelectedIndex = 0;
        }

        private void cmbTransactionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update UI based on transaction type if needed
        }

        private void cmbItemType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbItem.ItemsSource = null;
            _selectedItem = null;
            txtCurrentStockInfo.Text = "Select an item to see current stock information";
            
            if (cmbItemType.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString() == "Product")
                {
                    cmbItem.ItemsSource = _allProducts;
                }
                else if (selectedItem.Content.ToString() == "Component")
                {
                    cmbItem.ItemsSource = _allComponents;
                }
            }
        }

        private void cmbItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbItem.SelectedItem != null)
            {
                if (cmbItem.SelectedItem is Product product)
                {
                    _selectedItem = new InventoryItem
                    {
                        Id = product.Id,
                        Name = product.Name,
                        SKU = product.SKU,
                        CurrentStock = 0, // Will be calculated from location inventory
                        MinimumStock = 0, // Will be calculated from location inventory
                        ItemType = "Product"
                    };
                }
                else if (cmbItem.SelectedItem is Component component)
                {
                    _selectedItem = new InventoryItem
                    {
                        Id = component.Id,
                        Name = component.Name,
                        SKU = component.SKU,
                        CurrentStock = 0, // Will be calculated from location inventory
                        MinimumStock = 0, // Will be calculated from location inventory
                        ItemType = "Component"
                    };
                }
                
                UpdateStockInfo();
            }
        }

        private void UpdateStockInfo()
        {
            if (_selectedItem != null)
            {
                var status = _selectedItem.CurrentStock <= 0 ? "Out of Stock" :
                            _selectedItem.CurrentStock <= _selectedItem.MinimumStock ? "Low Stock" : "In Stock";
                
                txtCurrentStockInfo.Text = $"Current Stock: {_selectedItem.CurrentStock}\n" +
                                         $"Minimum Stock: {_selectedItem.MinimumStock}\n" +
                                         $"Status: {status}";
            }
        }

        private void txtQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow numbers and minus sign
            var regex = new Regex(@"^-?\d*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                SaveTransaction();
                ErrorDialog.ShowSuccess("Transaction saved successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving transaction: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
            if (cmbTransactionType.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select a transaction type.", "Validation Error");
                return false;
            }

            if (cmbItemType.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select an item type.", "Validation Error");
                return false;
            }

            if (cmbItem.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select an item.", "Validation Error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtQuantity.Text))
            {
                ErrorDialog.ShowWarning("Please enter a quantity.", "Validation Error");
                return false;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity == 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid quantity.", "Validation Error");
                return false;
            }

            if (cmbLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select a location.", "Validation Error");
                return false;
            }

            // Validate stock for sales
            var transactionType = (cmbTransactionType.SelectedItem as ComboBoxItem)?.Content.ToString();
                        if (transactionType == "Sale" && quantity > _selectedItem.CurrentStock)
            {
                ErrorDialog.ShowWarning($"Cannot sell {quantity} items. Only {_selectedItem.CurrentStock} available in stock.", "Insufficient Stock");
                return false;
            }

            return true;
        }

        private void SaveTransaction()
        {
            var transactionType = (cmbTransactionType.SelectedItem as ComboBoxItem)?.Content.ToString();
            var location = cmbLocation.SelectedItem as Location;
            var quantity = int.Parse(txtQuantity.Text);
            var notes = txtNotes.Text;

            using var connection = _databaseContext.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Insert transaction record
                var insertSql = @"
                    INSERT INTO InventoryTransactions 
                    (TransactionDate, TransactionType, LocationId, ProductId, ComponentId, Quantity, Notes) 
                    VALUES (@date, @type, @locationId, @productId, @componentId, @quantity, @notes)";

                using var command = new MySqlCommand(insertSql, connection);
                command.Transaction = transaction;
                command.Parameters.AddWithValue("@date", DateTime.Now);
                command.Parameters.AddWithValue("@type", transactionType);
                command.Parameters.AddWithValue("@locationId", location.Id);
                command.Parameters.AddWithValue("@productId", _selectedItem.ItemType == "Product" ? _selectedItem.Id : (object)DBNull.Value);
                command.Parameters.AddWithValue("@componentId", _selectedItem.ItemType == "Component" ? _selectedItem.Id : (object)DBNull.Value);
                command.Parameters.AddWithValue("@quantity", quantity);
                command.Parameters.AddWithValue("@notes", notes);

                command.ExecuteNonQuery();

                // Update stock
                var stockChange = transactionType switch
                {
                    "Sale" => -quantity,
                    "Purchase" => quantity,
                    "Transfer" => quantity, // Assuming transfer in
                    "Adjustment" => quantity,
                    "Return" => quantity,
                    "BreakDown" => -quantity,
                    "Damage" => -quantity,
                    "Expiry" => -quantity,
                    _ => 0
                };

                if (stockChange != 0)
                {
                    var tableName = _selectedItem.ItemType == "Product" ? "Products" : "Components";
                    var updateSql = $"UPDATE {tableName} SET CurrentStock = CurrentStock + @change, LastModified = @date WHERE Id = @id";
                    
                    using var updateCommand = new MySqlCommand(updateSql, connection);
                    updateCommand.Transaction = transaction;
                    updateCommand.Parameters.AddWithValue("@change", stockChange);
                    updateCommand.Parameters.AddWithValue("@date", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@id", _selectedItem.Id);
                    
                    updateCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 