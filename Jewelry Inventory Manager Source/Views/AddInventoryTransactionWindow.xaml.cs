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
    public partial class AddInventoryTransactionWindow : Window
    {
        private List<Product> _allProducts;
        private List<Component> _allComponents;
        private List<Location> _allLocations;
        private InventoryItem _selectedItem;
        private readonly DatabaseContext _databaseContext;

        public AddInventoryTransactionWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                LoadTransactionTypes();
                LoadItemTypes();
                LoadLocations();
                InitializeFilteredComboBox();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void LoadTransactionTypes()
        {
            cmbTransactionType.Items.Clear();
            cmbTransactionType.Items.Add(new ComboBoxItem { Content = "Add Stock", Tag = "AddStock" });
            cmbTransactionType.Items.Add(new ComboBoxItem { Content = "Remove Stock", Tag = "RemoveStock" });
            cmbTransactionType.Items.Add(new ComboBoxItem { Content = "Adjustment", Tag = "Adjustment" });
            cmbTransactionType.Items.Add(new ComboBoxItem { Content = "Return", Tag = "Return" });
            cmbTransactionType.Items.Add(new ComboBoxItem { Content = "Transfer", Tag = "Transfer" });
        }

        private void LoadItemTypes()
        {
            cmbItemType.Items.Clear();
            cmbItemType.Items.Add(new ComboBoxItem { Content = "Component", Tag = "Component" });
            cmbItemType.Items.Add(new ComboBoxItem { Content = "Product", Tag = "Product" });
            
            // Default to Component since products have special handling
            cmbItemType.SelectedIndex = 0;
        }

        private void LoadLocations()
        {
            try
            {
                var locations = _databaseContext.GetAllLocations();
                cmbLocation.Items.Clear();
                cmbLocation.Items.Add(new ComboBoxItem { Content = "Select Location", Tag = (int?)null });
                
                foreach (var location in locations.Where(l => l.IsActive))
                {
                    cmbLocation.Items.Add(new ComboBoxItem 
                    { 
                        Content = location.Name, 
                        Tag = location.Id 
                    });
                }
                
                cmbLocation.SelectedIndex = 0;
                cmbLocation.SelectionChanged += cmbLocation_SelectionChanged;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading locations: {ex.Message}", "Database Error");
            }
        }

        private void cmbLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update stock information when location changes
            UpdateStockInformation();
        }

        private void InitializeFilteredComboBox()
        {
            try
            {
                // Load all products and components
                _allProducts = _databaseContext.GetAllProducts().Where(p => p.IsActive).ToList();
                _allComponents = _databaseContext.GetAllComponents().Where(c => c.IsActive).ToList();
                _allLocations = _databaseContext.GetAllLocations().Where(l => l.IsActive).ToList();
                
                // Create filter service
                var filterService = new ItemFilterService(_databaseContext);
                
                // Initialize the filtered combo box
                filteredItemComboBox.Initialize(filterService, _allProducts, _allComponents);
                
                // Set default to components
                filteredItemComboBox.SetItemType(false);
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error initializing filtered combo box: {ex.Message}", "Error");
            }
        }

        private void cmbTransactionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTransactionType.SelectedItem is ComboBoxItem selectedItem)
            {
                var transactionType = selectedItem.Tag?.ToString() ?? "Adjustment";
                UpdateTransactionTypeExplanation(transactionType);
            }
        }

        private void UpdateTransactionTypeExplanation(string transactionType)
        {
            var explanation = transactionType switch
            {
                "AddStock" => "📦 Add Stock: Add items to inventory (e.g., receiving new components, found extra items).",
                "RemoveStock" => "💰 Remove Stock: Remove items from inventory (e.g., using components in production, damage, loss).",
                "Adjustment" => "⚖️ Adjustment: Correct inventory discrepancies (e.g., found extra items, damage, etc.).",
                "Return" => "🔄 Return: Return items to inventory. For products, this will restock their components automatically.",
                "Transfer" => "🚚 Transfer: Move stock between locations.",
                _ => "Select a transaction type to see its description"
            };
            
            txtTransactionTypeExplanation.Text = explanation;
        }

        private void cmbItemType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedItem = null;
            txtCurrentStockInfo.Text = "Select an item to see current stock information";

            if (cmbItemType.SelectedItem is ComboBoxItem selectedItem)
            {
                var itemType = selectedItem.Tag?.ToString() ?? "Component";
                
                if (itemType == "Product")
                {
                    // Check if this is a Return transaction - products can be returned
                    var transactionType = (cmbTransactionType.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                    if (transactionType == "Return")
                    {
                        // Allow product returns
                        filteredItemComboBox.SetItemType(true);
                        return;
                    }
                    else
                    {
                        // Show warning for other transaction types
                        ErrorDialog.ShowWarning(
                            "⚠️ Product Stock Warning\n\n" +
                            "Products should not have direct stock adjustments except for returns.\n\n" +
                            "For other transactions, you should:\n" +
                            "• Add/remove component stock instead\n" +
                            "• Use component transformations\n" +
                            "• Transfer components between locations\n\n" +
                            "Please select 'Component' as the item type for inventory transactions, or use 'Return' for product returns.",
                            "Product Stock Adjustment Not Allowed"
                        );
                        
                        // Reset to Component
                        cmbItemType.SelectedIndex = 0;
                        return;
                    }
                }
                
                if (itemType == "Component")
                {
                    filteredItemComboBox.SetItemType(false);
                }
                else
                {
                    filteredItemComboBox.SetItemType(true);
                }
            }
        }

        private void FilteredItemComboBox_SelectionChanged(object sender, object selectedItem)
        {
            if (selectedItem is ComboBoxDisplayItem displayItem)
            {
                if (displayItem.Item is Product product)
                {
                    _selectedItem = new InventoryItem
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Type = "Product",
                        CurrentStock = product.CurrentStock,
                        MinimumStock = product.MinimumStock
                    };
                    
                    UpdateStockInformation();
                }
                else if (displayItem.Item is Component component)
                {
                    _selectedItem = new InventoryItem
                    {
                        Id = component.Id,
                        Name = component.Name,
                        Type = "Component",
                        CurrentStock = component.CurrentStock,
                        MinimumStock = component.MinimumStock
                    };
                    
                    UpdateStockInformation();
                }
            }
            else
            {
                _selectedItem = null;
                txtCurrentStockInfo.Text = "Select an item to see current stock information";
            }
        }

        private void UpdateStockInformation()
        {
            if (_selectedItem == null)
            {
                txtCurrentStockInfo.Text = "Select an item to see current stock information";
                return;
            }

            var selectedLocationId = GetSelectedLocationId();
            var selectedLocation = _allLocations.FirstOrDefault(l => l.Id == selectedLocationId);
            var locationName = selectedLocation?.Name ?? "No Location Selected";

            if (_selectedItem.Type == "Product")
            {
                var product = _databaseContext.GetAllProducts().FirstOrDefault(p => p.Id == _selectedItem.Id);
                if (product != null)
                {
                    if (selectedLocationId > 0)
                    {
                        var locationStock = _databaseContext.GetProductStock(product.Id, selectedLocationId);
                        txtCurrentStockInfo.Text = $"Product: {product.Name}\n" +
                                                 $"Location: {locationName}\n" +
                                                 $"Calculated Stock: {locationStock} (based on component availability)\n" +
                                                 $"Note: Product stock is calculated automatically from component stock levels.";
                    }
                    else
                    {
                        // Show total across all locations if no specific location selected
                        var totalStock = 0;
                        foreach (var location in _allLocations)
                        {
                            totalStock += _databaseContext.GetProductStock(product.Id, location.Id);
                        }
                        txtCurrentStockInfo.Text = $"Product: {product.Name}\n" +
                                                 $"Total Stock (All Locations): {totalStock} (based on component availability)\n" +
                                                 $"Note: Product stock is calculated automatically from component stock levels.\n" +
                                                 $"Select a location to see location-specific stock.";
                    }
                }
            }
            else if (_selectedItem.Type == "Component")
            {
                var component = _databaseContext.GetAllComponents().FirstOrDefault(c => c.Id == _selectedItem.Id);
                if (component != null)
                {
                    if (selectedLocationId > 0)
                    {
                        var locationStock = _databaseContext.GetComponentStock(component.Id, selectedLocationId);
                        txtCurrentStockInfo.Text = $"Component: {component.Name}\n" +
                                                 $"Location: {locationName}\n" +
                                                 $"Current Stock: {locationStock}";
                    }
                    else
                    {
                        // Show breakdown by location if no specific location selected
                        var locationDetails = new List<string>();
                        var totalStock = 0;
                        
                        foreach (var location in _allLocations)
                        {
                            var stock = _databaseContext.GetComponentStock(component.Id, location.Id);
                            totalStock += stock;
                            locationDetails.Add($"{location.Name}: {stock}");
                        }
                        
                        txtCurrentStockInfo.Text = $"Component: {component.Name}\n" +
                                                 $"Total Stock (All Locations): {totalStock}\n" +
                                                 $"By Location:\n{string.Join("\n", locationDetails)}\n" +
                                                 $"Select a location to see location-specific stock.";
                    }
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;
                
            SaveTransaction();
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

            // Check if this is a product transaction
            var isProduct = cmbItemType.SelectedItem is ComboBoxItem selectedItem && 
                           selectedItem.Tag?.ToString() == "Product";
            var transactionType = (cmbTransactionType.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            // Only allow product returns, not other transaction types
            if (isProduct && transactionType != "Return")
            {
                ErrorDialog.ShowWarning(
                    "Products can only be returned, not directly adjusted.\n\n" +
                    "For other transactions, please select 'Component' as the item type and adjust component stock instead.",
                    "Product Transaction Not Allowed"
                );
                return false;
            }

            if (_selectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select an item.", "Validation Error");
                return false;
            }

            if (cmbLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select a location.", "Validation Error");
                return false;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid quantity greater than 0.", "Validation Error");
                return false;
            }

            return true;
        }

        private void SaveTransaction()
        {
            try
            {
                var transactionType = (cmbTransactionType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Adjustment";
                var quantity = int.Parse(txtQuantity.Text);
                var isProduct = GetSelectedItemType() == "Product";
                
                if (isProduct && transactionType == "Return")
                {
                    // Handle product return by restocking its components
                    HandleProductReturn(quantity);
                }
                else
                {
                    // Handle normal component transaction
                    var transaction = new InventoryTransaction
                    {
                        TransactionType = GetTransactionType(transactionType),
                        ItemType = GetSelectedItemType(),
                        ItemId = GetSelectedItemId(),
                        LocationId = GetSelectedLocationId(),
                        Quantity = GetTransactionQuantity(transactionType, quantity),
                        Notes = txtNotes.Text.Trim(),
                        TransactionDate = DateTime.Now
                    };
                    
                    _databaseContext.SaveTransaction(transaction);
                }
                
                // For product returns, the success message is already shown in HandleProductReturn
                // For other transactions, show the standard success message
                if (!(isProduct && transactionType == "Return"))
                {
                    ErrorDialog.ShowSuccess("Transaction saved successfully!", "Success");
                }
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving transaction: {ex.Message}", "Database Error");
            }
        }

        private void HandleProductReturn(int quantity)
        {
            try
            {
                var productId = GetSelectedItemId();
                var locationId = GetSelectedLocationId();
                var notes = txtNotes.Text.Trim();
                
                // Get the product and its components
                var product = _databaseContext.GetAllProducts().FirstOrDefault(p => p.Id == productId);
                if (product == null)
                {
                    throw new Exception("Product not found");
                }
                
                // Get the product's components
                var productComponents = _databaseContext.GetProductComponents(productId);
                if (!productComponents.Any())
                {
                    throw new Exception("Product has no components defined");
                }
                
                // Create transactions to restock each component
                foreach (var component in productComponents)
                {
                    var componentTransaction = new InventoryTransaction
                    {
                        TransactionType = TransactionType.Return,
                        ItemType = "Component",
                        ItemId = component.ComponentId,
                        LocationId = locationId,
                        Quantity = component.Quantity * quantity, // Restock the component quantity used in the product
                        Notes = $"Product return restock: {product.Name} (Qty: {quantity}) - {notes}",
                        TransactionDate = DateTime.Now
                    };
                    
                    _databaseContext.SaveTransaction(componentTransaction);
                }
                
                // Get location information
                var location = _allLocations.FirstOrDefault(l => l.Id == locationId);
                var locationName = location?.Name ?? "Unknown Location";
                
                // Build detailed component list for the success message
                var componentDetails = new List<string>();
                foreach (var component in productComponents)
                {
                    var restockQuantity = component.Quantity * quantity;
                    componentDetails.Add($"• {component.Component.Name} (SKU: {component.Component.SKU}): {restockQuantity} units");
                }
                
                var componentList = string.Join("\n", componentDetails);
                
                ErrorDialog.ShowSuccess(
                    $"Product return processed successfully!\n\n" +
                    $"Returned {quantity} x {product.Name}\n" +
                    $"Location: {locationName}\n" +
                    $"Restocked {productComponents.Count()} component types:\n\n" +
                    $"{componentList}\n\n" +
                    $"Total components restocked: {productComponents.Sum(c => c.Quantity * quantity)}",
                    "Product Return Completed"
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing product return: {ex.Message}");
            }
        }

        private TransactionType GetTransactionType(string transactionType)
        {
            return transactionType switch
            {
                "AddStock" => TransactionType.Purchase,
                "RemoveStock" => TransactionType.Sale,
                "Adjustment" => TransactionType.Adjustment,
                "Return" => TransactionType.Return,
                "Transfer" => TransactionType.Transfer,
                _ => TransactionType.Adjustment
            };
        }

        private int GetTransactionQuantity(string transactionType, int quantity)
        {
            return transactionType switch
            {
                "RemoveStock" => -quantity, // Negative for removing stock
                "Return" => quantity, // Positive for returns
                _ => quantity // Positive for adding stock, adjustments, and transfers
            };
        }

        private string GetSelectedItemType()
        {
            if (cmbItemType.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag?.ToString() ?? "Component";
            }
            return "Component";
        }

        private int GetSelectedItemId()
        {
            return _selectedItem?.Id ?? 0;
        }

        private int GetSelectedLocationId()
        {
            if (cmbLocation.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag as int? ?? 0;
            }
            return 0;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtQuantity_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers
            var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
} 