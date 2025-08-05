using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using Moonglow_DB.Views;

namespace Moonglow_DB.Views
{
    public partial class TransferWindow : Window
    {
        private List<TransferItem> _transferItems;
        private List<InventoryItem> _allInventoryItems;
        private List<Location> _allLocations;
        private TransferItem _selectedTransferItem;

        public TransferWindow()
        {
            InitializeComponent();
            _transferItems = new List<TransferItem>();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using var context = new DatabaseContext(connectionString);
                
                LoadInventoryItems(context);
                LoadLocations(context);
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading data: {ex.Message}", "Data Load Error");
            }
        }

        private void LoadInventoryItems(DatabaseContext context)
        {
            try
            {
                _allInventoryItems = new List<InventoryItem>();
                
                // Load products
                var products = context.GetAllProducts();
                foreach (var product in products)
                {
                    _allInventoryItems.Add(new InventoryItem
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Type = "Product",
                        SKU = product.SKU,
                        CurrentStock = 0 // This would need to be calculated from inventory
                    });
                }
                
                // Load components
                var components = context.GetAllComponents();
                foreach (var component in components)
                {
                    _allInventoryItems.Add(new InventoryItem
                    {
                        Id = component.Id,
                        Name = component.Name,
                        Type = "Component",
                        SKU = component.SKU,
                        CurrentStock = 0 // This would need to be calculated from inventory
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading inventory items: {ex.Message}", "Data Load Error");
            }
        }

        private void LoadLocations(DatabaseContext context)
        {
            try
            {
                _allLocations = context.GetAllLocations();
                cmbSourceLocation.ItemsSource = _allLocations;
                cmbDestinationLocation.ItemsSource = _allLocations;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading locations: {ex.Message}", "Data Load Error");
            }
        }

        private void UpdateTransferList()
        {
            dgTransferItems.ItemsSource = null;
            dgTransferItems.ItemsSource = _transferItems;
        }

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSourceLocation.SelectedItem == null || cmbDestinationLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select both source and destination locations.", "Validation Error");
                return;
            }

            if (cmbItem.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select an item to transfer.", "Validation Error");
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid quantity.", "Validation Error");
                return;
            }

            var selectedItem = cmbItem.SelectedItem as InventoryItem;
            var sourceLocation = cmbSourceLocation.SelectedItem as Location;
            var destinationLocation = cmbDestinationLocation.SelectedItem as Location;

            var transferItem = new TransferItem
            {
                ItemId = selectedItem.Id,
                ItemName = selectedItem.Name,
                ItemType = selectedItem.Type,
                SKU = selectedItem.SKU,
                Quantity = quantity,
                FromLocation = sourceLocation.Name,
                ToLocation = destinationLocation.Name,
                CurrentStock = selectedItem.CurrentStock
            };

            _transferItems.Add(transferItem);
            UpdateTransferList();
            ClearForm();
        }

        private void btnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTransferItem == null)
            {
                ErrorDialog.ShowInformation("Please select an item to remove.", "No Selection");
                return;
            }

            _transferItems.Remove(_selectedTransferItem);
            UpdateTransferList();
            _selectedTransferItem = null;
        }

        private void btnExecuteTransfer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using var dbContext = new DatabaseContext(connectionString);
                
                foreach (var item in _transferItems)
                {
                    // Find source and destination locations by name
                    var sourceLocation = _allLocations.FirstOrDefault(l => l.Name == item.FromLocation);
                    var destinationLocation = _allLocations.FirstOrDefault(l => l.Name == item.ToLocation);
                    
                    if (sourceLocation == null || destinationLocation == null)
                    {
                        ErrorDialog.ShowError($"Could not find location for transfer item {item.ItemName}", "Transfer Error");
                        return;
                    }
                    
                    // Create outbound transaction
                    var outboundTransaction = new InventoryTransaction
                    {
                        TransactionType = TransactionType.TransferOut,
                        ItemType = item.ItemType,
                        ItemId = item.ItemId,
                        LocationId = sourceLocation.Id,
                        Quantity = -item.Quantity,
                        Notes = $"Transfer to {item.ToLocation}",
                        TransactionDate = DateTime.Now
                    };
                    
                    // Create inbound transaction
                    var inboundTransaction = new InventoryTransaction
                    {
                        TransactionType = TransactionType.TransferIn,
                        ItemType = item.ItemType,
                        ItemId = item.ItemId,
                        LocationId = destinationLocation.Id,
                        Quantity = item.Quantity,
                        Notes = $"Transfer from {item.FromLocation}",
                        TransactionDate = DateTime.Now
                    };
                    
                    dbContext.SaveTransaction(outboundTransaction);
                    dbContext.SaveTransaction(inboundTransaction);
                }
                
                ErrorDialog.ShowSuccess("Transfer completed successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error executing transfer: {ex.Message}", "Database Error");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ValidateTransfer()
        {
            if (cmbSourceLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select a source location.", "Validation Error");
                return;
            }

            if (cmbDestinationLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select a destination location.", "Validation Error");
                return;
            }

            if (cmbSourceLocation.SelectedItem == cmbDestinationLocation.SelectedItem)
            {
                ErrorDialog.ShowWarning("Source and destination locations cannot be the same.", "Validation Error");
                return;
            }
        }

        private void ClearForm()
        {
            cmbItem.SelectedItem = null;
            txtQuantity.Text = string.Empty;
        }

        private void dgTransferItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedTransferItem = dgTransferItems.SelectedItem as TransferItem;
        }

        private void cmbSourceLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAvailableItems();
        }

        private void cmbDestinationLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAvailableItems();
        }

        private void cmbItemType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAvailableItems();
        }

        private void cmbItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update quantity field or other UI elements as needed
        }

        private void txtQuantity_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numeric input
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            btnExecuteTransfer_Click(sender, e);
        }

        private void UpdateAvailableItems()
        {
            if (cmbSourceLocation.SelectedItem == null || cmbDestinationLocation.SelectedItem == null)
                return;

            var sourceLocation = cmbSourceLocation.SelectedItem as Location;
            var destinationLocation = cmbDestinationLocation.SelectedItem as Location;

            // Filter items based on selected locations and type
            var availableItems = _allInventoryItems.Where(item => 
                item.CurrentStock > 0 && 
                (cmbItemType.SelectedItem == null || item.Type == cmbItemType.SelectedItem.ToString()));

            cmbItem.ItemsSource = availableItems;
        }

        private string GetLocationName(int locationId)
        {
            var location = _allLocations.FirstOrDefault(l => l.Id == locationId);
            return location?.Name ?? "Unknown Location";
        }
    }
} 