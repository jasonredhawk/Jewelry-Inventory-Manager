using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;
using Moonglow_DB.Views.Controls;

namespace Moonglow_DB.Views
{
    public partial class InventoryTransferWindow : Window
    {
        private List<Product> _allProducts;
        private List<Component> _allComponents;
        private List<Location> _allLocations;
        private int _availableStock;
        private object _selectedItem;
        private readonly DatabaseContext _databaseContext;
        private List<TransferListItem> _transferItems;

        public InventoryTransferWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _transferItems = new List<TransferListItem>();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _allLocations = _databaseContext.GetAllLocations().Where(l => l.IsActive).ToList();
                LoadLocations();
                LoadItemTypes();
                InitializeFilteredComboBox();
                UpdateTransferSummary();
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
                cmbFromLocation.Items.Clear();
                cmbToLocation.Items.Clear();
                
                cmbFromLocation.Items.Add(new ComboBoxItem { Content = "Select Source Location", Tag = (int?)null });
                cmbToLocation.Items.Add(new ComboBoxItem { Content = "Select Destination Location", Tag = (int?)null });
                
                foreach (var location in _allLocations)
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
            cmbItemType.SelectedIndex = 0; // Default to Product
        }

        private void InitializeFilteredComboBox()
        {
            try
            {
                // Load all products and components
                _allProducts = _databaseContext.GetAllProducts().Where(p => p.IsActive).ToList();
                _allComponents = _databaseContext.GetAllComponents().Where(c => c.IsActive).ToList();
                
                // Create filter service
                var filterService = new ItemFilterService(_databaseContext);
                
                // Initialize the filtered combo box
                filteredItemComboBox.Initialize(filterService, _allProducts, _allComponents);
                
                // Set default to products
                filteredItemComboBox.SetItemType(true);
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error initializing filtered combo box: {ex.Message}", "Error");
            }
        }

        private void cmbItemType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtAvailableStock.Text = string.Empty;
            _availableStock = 0;
            _selectedItem = null;

            if (cmbItemType.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString() == "Product")
                {
                    filteredItemComboBox.SetItemType(true);
                }
                else if (selectedItem.Content.ToString() == "Component")
                {
                    filteredItemComboBox.SetItemType(false);
                }
            }
        }

        private void FilteredItemComboBox_SelectionChanged(object sender, object selectedItem)
        {
            _selectedItem = selectedItem;
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
                if (_selectedItem == null || cmbFromLocation.SelectedItem == null) 
                {
                    txtAvailableStock.Text = "0";
                    _availableStock = 0;
                    return;
                }

                var fromLocationItem = cmbFromLocation.SelectedItem as ComboBoxItem;
                if (fromLocationItem?.Tag == null) return;

                var locationId = (int)fromLocationItem.Tag;
                var itemId = 0;

                if (_selectedItem is ComboBoxDisplayItem displayItem)
                {
                    if (displayItem.Item is Product product)
                    {
                        itemId = product.Id;
                        // Use GetProductStock which calculates stock from component availability
                        _availableStock = _databaseContext.GetProductStock(itemId, locationId);
                    }
                    else if (displayItem.Item is Component component)
                    {
                        itemId = component.Id;
                        // Use GetComponentStock which gets stock directly from LocationInventory
                        _availableStock = _databaseContext.GetComponentStock(itemId, locationId);
                    }
                }

                txtAvailableStock.Text = _availableStock.ToString();
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

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAddItem()) return;

            try
            {
                var transferItem = CreateTransferListItem();
                if (transferItem != null)
                {
                    _transferItems.Add(transferItem);
                    lstTransferItems.ItemsSource = null;
                    lstTransferItems.ItemsSource = _transferItems;
                    UpdateTransferSummary();
                    ClearItemForm();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error adding item to transfer: {ex.Message}", "Error");
            }
        }

        private bool ValidateAddItem()
        {
            if (cmbFromLocation.SelectedItem == null || cmbToLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select both source and destination locations.", "Validation Error");
                return false;
            }

            if (_selectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select an item to transfer.", "Validation Error");
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

            var fromLocationId = GetSelectedFromLocationId();
            var toLocationId = GetSelectedToLocationId();
            if (fromLocationId == toLocationId)
            {
                ErrorDialog.ShowWarning("Source and destination locations must be different.", "Validation Error");
                return false;
            }

            return true;
        }

        private TransferListItem CreateTransferListItem()
        {
            if (_selectedItem is ComboBoxDisplayItem displayItem)
            {
                var transferItem = new TransferListItem
                {
                    Quantity = int.Parse(txtTransferQuantity.Text),
                    AvailableStock = _availableStock
                };

                if (displayItem.Item is Product product)
                {
                    transferItem.ItemId = product.Id;
                    transferItem.ItemType = "Product";
                    transferItem.DisplayName = product.Name;
                    transferItem.SKU = product.SKU;

                    // Get component breakdown for products
                    var components = _databaseContext.GetProductComponents(product.Id);
                    if (components.Any())
                    {
                        var componentList = components.Select(c => $"{c.Component.Name} (x{c.Quantity})");
                        transferItem.ComponentBreakdown = string.Join(", ", componentList);
                    }
                }
                else if (displayItem.Item is Component component)
                {
                    transferItem.ItemId = component.Id;
                    transferItem.ItemType = "Component";
                    transferItem.DisplayName = component.Name;
                    transferItem.SKU = component.SKU;
                }

                return transferItem;
            }

            return null;
        }

        private void ClearItemForm()
        {
            filteredItemComboBox.ClearSelection();
            txtTransferQuantity.Text = "1";
            txtAvailableStock.Text = "0";
            _selectedItem = null;
            _availableStock = 0;
        }

        private void btnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TransferListItem item)
            {
                _transferItems.Remove(item);
                lstTransferItems.ItemsSource = null;
                lstTransferItems.ItemsSource = _transferItems;
                UpdateTransferSummary();
            }
        }

        private void UpdateTransferSummary()
        {
            if (_transferItems.Count == 0)
            {
                txtTransferSummary.Text = "No items in transfer list";
                btnCreateTransfer.IsEnabled = false;
                return;
            }

            var totalItems = _transferItems.Count;
            var totalQuantity = _transferItems.Sum(item => item.Quantity);
            var products = _transferItems.Count(item => item.IsProduct);
            var components = _transferItems.Count(item => item.IsComponent);

            txtTransferSummary.Text = $"Total Items: {totalItems} | Total Quantity: {totalQuantity} | Products: {products} | Components: {components}";
            btnCreateTransfer.IsEnabled = true;
        }

        private void btnCreateTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCreateTransfer()) return;

            try
            {
                var transferOrder = CreateBulkTransferOrder();
                var transferId = _databaseContext.CreateBulkTransferOrder(transferOrder);

                // Add all items to the transfer
                foreach (var item in _transferItems)
                {
                    var transferItem = new BulkTransferItem
                    {
                        TransferOrderId = transferId,
                        ItemId = item.ItemId,
                        ItemType = item.ItemType,
                        ItemName = item.DisplayName,
                        SKU = item.SKU,
                        Quantity = item.Quantity,
                        AvailableStock = item.AvailableStock,
                        Notes = item.Notes
                    };

                    _databaseContext.AddBulkTransferItem(transferItem);
                }

                ErrorDialog.ShowSuccess($"Bulk transfer created successfully!\n\nTransfer Number: {transferOrder.TransferNumber}\nItems: {_transferItems.Count}\nStatus: Created\n\nYou can track and manage this transfer from the transfer management window.", "Transfer Created");
                
                // Clear the form
                _transferItems.Clear();
                lstTransferItems.ItemsSource = null;
                UpdateTransferSummary();
                txtNotes.Text = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error creating bulk transfer: {ex.Message}", "Error");
            }
        }

        private bool ValidateCreateTransfer()
        {
            if (_transferItems.Count == 0)
            {
                ErrorDialog.ShowWarning("Please add at least one item to the transfer list.", "Validation Error");
                return false;
            }

            if (cmbFromLocation.SelectedItem == null || cmbToLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select both source and destination locations.", "Validation Error");
                return false;
            }

            var fromLocationId = GetSelectedFromLocationId();
            var toLocationId = GetSelectedToLocationId();
            if (fromLocationId == toLocationId)
            {
                ErrorDialog.ShowWarning("Source and destination locations must be different.", "Validation Error");
                return false;
            }

            return true;
        }

        private BulkTransferOrder CreateBulkTransferOrder()
        {
            var fromLocationId = GetSelectedFromLocationId();
            var toLocationId = GetSelectedToLocationId();
            var fromLocation = _allLocations.FirstOrDefault(l => l.Id == fromLocationId);
            var toLocation = _allLocations.FirstOrDefault(l => l.Id == toLocationId);

            return new BulkTransferOrder
            {
                TransferNumber = _databaseContext.GenerateTransferNumber(),
                FromLocationId = fromLocationId,
                ToLocationId = toLocationId,
                FromLocationName = fromLocation?.Name ?? "Unknown",
                ToLocationName = toLocation?.Name ?? "Unknown",
                Status = TransferStatus.Created,
                Notes = txtNotes.Text,
                CreatedDate = DateTime.Now
            };
        }

        private void btnViewTransfers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var managementWindow = new BulkTransferManagementWindow(_databaseContext);
                managementWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening transfer management: {ex.Message}", "Error");
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

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 