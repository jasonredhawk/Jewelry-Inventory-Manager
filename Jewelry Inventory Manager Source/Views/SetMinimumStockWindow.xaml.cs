using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonglow_DB.Data;
using Moonglow_DB.Models;

namespace Moonglow_DB.Views
{
    public partial class SetMinimumStockWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<Location> _allLocations;
        private List<Product> _allProducts;
        private List<Component> _allComponents;
        private Location _selectedLocation;
        private string _selectedItemType;
        private int _selectedItemId;

        public SetMinimumStockWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadData();
            
            // Trigger initial item loading after window is loaded
            Loaded += (s, e) => LoadItems();
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
                ErrorDialog.ShowError($"Error in SetMinimumStockWindow.LoadData(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

        private void LoadLocations()
        {
            try
            {
                if (_databaseContext == null)
                {
                    ErrorDialog.ShowError("Database context is null in SetMinimumStockWindow.LoadLocations().", "Error");
                    return;
                }

                if (cmbLocation == null)
                {
                    ErrorDialog.ShowError("Location ComboBox is null in SetMinimumStockWindow.LoadLocations().", "Error");
                    return;
                }

                _allLocations = _databaseContext.GetAllLocations().Where(l => l.IsActive).ToList();
                cmbLocation.Items.Clear();
                
                foreach (var location in _allLocations)
                {
                    if (location != null)
                    {
                        var item = new ComboBoxItem
                        {
                            Content = location.Name,
                            Tag = location.Id
                        };
                        cmbLocation.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in SetMinimumStockWindow.LoadLocations(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

        private void LoadItemTypes()
        {
            try
            {
                if (cmbItemType == null)
                {
                    ErrorDialog.ShowError("Item type ComboBox is null in SetMinimumStockWindow.LoadItemTypes().", "Error");
                    return;
                }

                cmbItemType.Items.Clear();
                cmbItemType.Items.Add(new ComboBoxItem { Content = "Component" });
                cmbItemType.Items.Add(new ComboBoxItem { Content = "Product" });
                cmbItemType.SelectedIndex = 0;
                
                // Set the default item type directly
                _selectedItemType = "Component";
                
                // Don't load items here - let the SelectionChanged event handle it
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in SetMinimumStockWindow.LoadItemTypes(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

        private void LoadItems()
        {
            try
            {
                if (_databaseContext == null)
                {
                    ErrorDialog.ShowError("Database context is null in SetMinimumStockWindow.LoadItems().", "Error");
                    return;
                }

                if (cmbItem == null)
                {
                    ErrorDialog.ShowError("Item ComboBox is null in SetMinimumStockWindow.LoadItems().", "Error");
                    return;
                }

                cmbItem.Items.Clear();
                
                // Use the stored item type or get it from the ComboBox
                if (string.IsNullOrEmpty(_selectedItemType))
                {
                    _selectedItemType = GetSelectedItemType();
                }

                if (_selectedItemType == "Component")
                {
                    try
                    {
                        _allComponents = _databaseContext.GetAllComponents().Where(c => c.IsActive).ToList();
                        foreach (var component in _allComponents)
                        {
                            if (component != null)
                            {
                                var item = new ComboBoxItem
                                {
                                    Content = $"{component.SKU} - {component.Name}",
                                    Tag = component.Id
                                };
                                cmbItem.Items.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorDialog.ShowError($"Error loading components in SetMinimumStockWindow.LoadItems(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
                    }
                }
                else
                {
                    try
                    {
                        _allProducts = _databaseContext.GetAllProducts().Where(p => p.IsActive).ToList();
                        foreach (var product in _allProducts)
                        {
                            if (product != null)
                            {
                                var item = new ComboBoxItem
                                {
                                    Content = $"{product.SKU} - {product.Name}",
                                    Tag = product.Id
                                };
                                cmbItem.Items.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorDialog.ShowError($"Error loading products in SetMinimumStockWindow.LoadItems(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
                    }
                }
                
                // Clear current info when items are reloaded
                ClearCurrentInfo();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in SetMinimumStockWindow.LoadItems(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

        private string GetSelectedItemType()
        {
            try
            {
                if (cmbItemType.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
                {
                    return selectedItem.Content.ToString();
                }
            }
            catch
            {
                // If there's any issue, return the default
            }
            return "Component";
        }

        private int GetSelectedLocationId()
        {
            if (cmbLocation.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                return selectedItem.Tag as int? ?? 0;
            }
            return 0;
        }

        private int GetSelectedItemId()
        {
            if (cmbItem.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                return selectedItem.Tag as int? ?? 0;
            }
            return 0;
        }

        private void UpdateCurrentInfo()
        {
            try
            {
                var locationId = GetSelectedLocationId();
                var itemId = GetSelectedItemId();
                var itemType = GetSelectedItemType();

                if (locationId == 0 || itemId == 0)
                {
                    ClearCurrentInfo();
                    return;
                }

                // Get current stock
                int currentStock = 0;
                if (itemType == "Component")
                {
                    currentStock = _databaseContext.GetComponentStock(itemId, locationId);
                }
                else
                {
                    currentStock = _databaseContext.GetProductStock(itemId, locationId);
                }

                // Get current minimum stock
                int currentMinStock = _databaseContext.GetMinimumStock(itemType, itemId, locationId);

                // Get item name
                string itemName = "";
                if (itemType == "Component")
                {
                    var component = _allComponents?.FirstOrDefault(c => c.Id == itemId);
                    itemName = component?.Name ?? "";
                }
                else
                {
                    var product = _allProducts?.FirstOrDefault(p => p.Id == itemId);
                    itemName = product?.Name ?? "";
                }

                // Get location name
                string locationName = _selectedLocation?.Name ?? "";

                // Update display
                txtCurrentStock.Text = currentStock.ToString();
                txtCurrentMinStock.Text = currentMinStock.ToString();
                txtItemName.Text = itemName;
                txtLocationName.Text = locationName;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error updating current info: {ex.Message}", "Error");
                ClearCurrentInfo();
            }
        }

        private void ClearCurrentInfo()
        {
            txtCurrentStock.Text = "0";
            txtCurrentMinStock.Text = "0";
            txtItemName.Text = "";
            txtLocationName.Text = "";
        }

        private void cmbLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLocation.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                var locationId = selectedItem.Tag as int? ?? 0;
                _selectedLocation = _allLocations?.FirstOrDefault(l => l.Id == locationId);
                UpdateCurrentInfo();
            }
        }

        private void cmbItemType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the selected item type
            _selectedItemType = GetSelectedItemType();
            // Don't call UpdateCurrentInfo here as it will be called when an item is selected
        }

        private void cmbItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedItemId = GetSelectedItemId();
            UpdateCurrentInfo();
        }

        private void txtNewMinimumStock_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow numbers
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var locationId = GetSelectedLocationId();
                var itemId = GetSelectedItemId();
                var itemType = GetSelectedItemType();

                if (locationId == 0)
                {
                    ErrorDialog.ShowError("Please select a location.", "Validation Error");
                    return;
                }

                if (itemId == 0)
                {
                    ErrorDialog.ShowError("Please select an item.", "Validation Error");
                    return;
                }

                if (!int.TryParse(txtNewMinimumStock.Text, out int newMinimumStock))
                {
                    ErrorDialog.ShowError("Please enter a valid number for minimum stock.", "Validation Error");
                    return;
                }

                if (newMinimumStock < 0)
                {
                    ErrorDialog.ShowError("Minimum stock cannot be negative.", "Validation Error");
                    return;
                }

                // Set the minimum stock
                _databaseContext.SetMinimumStock(itemType, itemId, locationId, newMinimumStock);

                string message = newMinimumStock == 0 
                    ? $"Minimum stock removed for {txtItemName.Text} at {txtLocationName.Text}."
                    : $"Minimum stock set to {newMinimumStock} for {txtItemName.Text} at {txtLocationName.Text}.";

                ErrorDialog.ShowSuccess(message, "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving minimum stock: {ex.Message}", "Error");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 