using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class InventoryTransferWindow : Window
    {
        private List<Product> _allProducts;
        private List<Component> _allComponents;
        private List<Location> _allLocations;
        private int _availableStock;
        private readonly DatabaseContext _databaseContext;

        public InventoryTransferWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                LoadLocations();
                LoadItemTypes();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void LoadLocations()
        {
            try
            {
                var locations = _databaseContext.GetAllLocations();
                cmbFromLocation.Items.Clear();
                cmbToLocation.Items.Clear();
                
                cmbFromLocation.Items.Add(new ComboBoxItem { Content = "Select Source Location", Tag = (int?)null });
                cmbToLocation.Items.Add(new ComboBoxItem { Content = "Select Destination Location", Tag = (int?)null });
                
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    cmbFromLocation.Items.Add(new ComboBoxItem 
                    { 
                        Content = location.Name, 
                        Tag = location.Id 
                    });
                    cmbToLocation.Items.Add(new ComboBoxItem 
                    { 
                        Content = location.Name, 
                        Tag = location.Id 
                    });
                }
                
                cmbFromLocation.SelectedIndex = 0;
                cmbToLocation.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading locations: {ex.Message}", "Database Error");
            }
        }

        private void LoadItemTypes()
        {
            cmbItemType.Items.Clear();
            cmbItemType.Items.Add(new ComboBoxItem { Content = "Product" });
            cmbItemType.Items.Add(new ComboBoxItem { Content = "Component" });
        }

        private void LoadProducts()
        {
            try
            {
                var products = _databaseContext.GetAllProducts();
                cmbItem.Items.Clear();
                cmbItem.Items.Add(new ComboBoxItem { Content = "Select Product", Tag = (int?)null });
                
                foreach (var product in products.Where(p => p.IsActive))
                {
                    cmbItem.Items.Add(new ComboBoxItem 
                    { 
                        Content = $"{product.Name} (Stock: {product.CurrentStock})", 
                        Tag = product.Id 
                    });
                }
                
                cmbItem.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading products: {ex.Message}", "Database Error");
            }
        }

        private void LoadComponents()
        {
            try
            {
                var components = _databaseContext.GetAllComponents();
                cmbItem.Items.Clear();
                cmbItem.Items.Add(new ComboBoxItem { Content = "Select Component", Tag = (int?)null });
                
                foreach (var component in components.Where(c => c.IsActive))
                {
                    cmbItem.Items.Add(new ComboBoxItem 
                    { 
                        Content = $"{component.Name} (Stock: {component.CurrentStock})", 
                        Tag = component.Id 
                    });
                }
                
                cmbItem.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading components: {ex.Message}", "Database Error");
            }
        }

        private void cmbItemType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbItem.ItemsSource = null;
            cmbItem.SelectedIndex = -1;
            txtAvailableStock.Text = string.Empty;
            _availableStock = 0;

            if (cmbItemType.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString() == "Product")
                {
                    LoadProducts();
                }
                else if (selectedItem.Content.ToString() == "Component")
                {
                    LoadComponents();
                }
            }
        }

        private void cmbItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFromLocation.SelectedItem != null)
            {
                UpdateAvailableStock();
            }
        }

        private void cmbFromLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAvailableStock();
        }

        private void UpdateAvailableStock()
        {
            try
            {
                if (cmbItem.SelectedItem == null || cmbFromLocation.SelectedItem == null) return;

                var location = cmbFromLocation.SelectedItem as Location;
                var itemId = 0;
                var itemType = "";

                if (cmbItem.SelectedItem is Product product)
                {
                    itemId = product.Id;
                    itemType = "Product";
                }
                else if (cmbItem.SelectedItem is Component component)
                {
                    itemId = component.Id;
                    itemType = "Component";
                }

                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using (var context = new DatabaseContext(connectionString))
                {
                    var sql = @"
                        SELECT COALESCE(SUM(Quantity), 0) as AvailableStock
                        FROM LocationInventory 
                        WHERE LocationId = @locationId 
                        AND ItemId = @itemId 
                        AND ItemType = @itemType";

                    using (var command = context.CreateCommand(sql))
                    {
                        command.Parameters.AddWithValue("@locationId", location.Id);
                        command.Parameters.AddWithValue("@itemId", itemId);
                        command.Parameters.AddWithValue("@itemType", itemType);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _availableStock = reader.GetInt32(0);
                                txtAvailableStock.Text = _availableStock.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error updating available stock: {ex.Message}", "Error");
            }
        }

        private void txtTransferQuantity_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers
            var regex = new System.Text.RegularExpressions.Regex(@"^\d*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                ExecuteTransfer();
                ErrorDialog.ShowSuccess("Transfer completed successfully!", "Success");
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error executing transfer: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
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

            if (cmbFromLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select a source location.", "Validation Error");
                return false;
            }

            if (cmbToLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select a destination location.", "Validation Error");
                return false;
            }

            if (cmbFromLocation.SelectedItem == cmbToLocation.SelectedItem)
            {
                ErrorDialog.ShowWarning("Source and destination locations must be different.", "Validation Error");
                return false;
            }

            if (!int.TryParse(txtTransferQuantity.Text, out int quantity) || quantity <= 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid transfer quantity greater than 0.", "Validation Error");
                return false;
            }

            if (quantity > _availableStock)
            {
                ErrorDialog.ShowWarning($"Transfer quantity ({quantity}) cannot exceed available stock ({_availableStock}).", "Validation Error");
                return false;
            }

            return true;
        }

        private void ExecuteTransfer()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using var dbContext = new DatabaseContext(connectionString);
                
                var fromLocationId = GetSelectedFromLocationId();
                var toLocationId = GetSelectedToLocationId();
                var itemId = GetSelectedItemId();
                var itemType = GetSelectedItemType();
                var quantity = int.Parse(txtTransferQuantity.Text);
                
                // Create outbound transaction
                var outboundTransaction = new InventoryTransaction
                {
                    TransactionType = TransactionType.TransferOut,
                    ItemType = itemType,
                    ItemId = itemId,
                    LocationId = fromLocationId,
                    Quantity = -quantity,
                    Notes = $"Transfer to {GetLocationName(toLocationId)}",
                    TransactionDate = DateTime.Now
                };
                
                // Create inbound transaction
                var inboundTransaction = new InventoryTransaction
                {
                    TransactionType = TransactionType.TransferIn,
                    ItemType = itemType,
                    ItemId = itemId,
                    LocationId = toLocationId,
                    Quantity = quantity,
                    Notes = $"Transfer from {GetLocationName(fromLocationId)}",
                    TransactionDate = DateTime.Now
                };
                
                dbContext.SaveTransaction(outboundTransaction);
                dbContext.SaveTransaction(inboundTransaction);
                
                ErrorDialog.ShowSuccess("Transfer completed successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error executing transfer: {ex.Message}", "Database Error");
            }
        }

        private int GetSelectedFromLocationId()
        {
            return cmbFromLocation.SelectedItem is ComboBoxItem selectedFromLocation ? (int)selectedFromLocation.Tag : 0;
        }

        private int GetSelectedToLocationId()
        {
            return cmbToLocation.SelectedItem is ComboBoxItem selectedToLocation ? (int)selectedToLocation.Tag : 0;
        }

        private int GetSelectedItemId()
        {
            return cmbItem.SelectedItem is ComboBoxItem selectedItem ? (int)selectedItem.Tag : 0;
        }

        private string GetSelectedItemType()
        {
            return cmbItemType.SelectedItem is ComboBoxItem selectedItem ? selectedItem.Content.ToString() : string.Empty;
        }

        private string GetLocationName(int locationId)
        {
            var settings = SettingsManager.LoadSettings();
            var connectionString = SettingsManager.BuildConnectionString(settings);
            using var dbContext = new DatabaseContext(connectionString);
            var location = dbContext.GetLocationById(locationId);
            return location?.Name ?? "Unknown Location";
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 