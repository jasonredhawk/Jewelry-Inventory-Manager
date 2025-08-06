using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using Moonglow_DB.Views.Controls;

namespace Moonglow_DB.Views
{
    public partial class SetStockLevelsWindow : Window
    {
                 private readonly DatabaseContext _databaseContext;
         private List<Location> _allLocations;
         private List<Product> _allProducts;
         private List<Component> _allComponents;
         private List<Category> _allCategories;
         private List<StockLevelItem> _allStockItems;
         
         // Selected item variables
         private Location _selectedLocation;
         private string _selectedItemType;
         private Category _selectedCategory;
         private StockLevelItem _selectedStockItem;

        public SetStockLevelsWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadData();
        }

                 private void LoadData()
         {
             try
             {
                 // Initialize the stock items list first
                 _allStockItems = new List<StockLevelItem>();
                 
                 LoadLocations();
                 LoadItemTypes();
                 LoadCategories();
                 LoadAllStockItems();
             }
             catch (Exception ex)
             {
                 ErrorDialog.ShowError($"Error in SetStockLevelsWindow.LoadData(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
             }
         }

        private void LoadLocations()
        {
            try
            {
                if (_databaseContext == null)
                {
                    ErrorDialog.ShowError("Database context is null in SetStockLevelsWindow.LoadLocations().", "Error");
                    return;
                }

                _allLocations = _databaseContext.GetAllLocations().Where(l => l.IsActive).ToList();
                
                // Load locations for the combo box
                LoadLocationComboBox(cmbLocation);
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in SetStockLevelsWindow.LoadLocations(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

                 private void LoadLocationComboBox(ComboBox comboBox)
         {
             if (comboBox == null) return;
             
             comboBox.Items.Clear();
             
             // Add "All Locations" option
             comboBox.Items.Add(new ComboBoxItem
             {
                 Content = "All Locations",
                 Tag = 0
             });
             
             // Add individual locations
             foreach (var location in _allLocations)
             {
                 if (location != null)
                 {
                     var item = new ComboBoxItem
                     {
                         Content = location.Name,
                         Tag = location.Id
                     };
                     comboBox.Items.Add(item);
                 }
             }
             
             // Select "All Locations" by default
             comboBox.SelectedIndex = 0;
         }

        private void LoadItemTypes()
        {
            try
            {
                // Load item types for the combo box
                LoadItemTypeComboBox(cmbItemType);
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in SetStockLevelsWindow.LoadItemTypes(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

                 private void LoadItemTypeComboBox(ComboBox comboBox)
         {
             if (comboBox == null) return;
             
             comboBox.Items.Clear();
             comboBox.Items.Add(new ComboBoxItem { Content = "Product & Component" });
             comboBox.Items.Add(new ComboBoxItem { Content = "Product" });
             comboBox.Items.Add(new ComboBoxItem { Content = "Component" });
             comboBox.SelectedIndex = 0;
         }

                 private void LoadCategories()
         {
             try
             {
                 _allCategories = _databaseContext.GetAllCategories().Where(c => c.IsActive).ToList();
                 LoadCategoryComboBox(cmbCategory);
             }
             catch (Exception ex)
             {
                 ErrorDialog.ShowError($"Error in SetStockLevelsWindow.LoadCategories(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
             }
         }

         private void LoadCategoryComboBox(ComboBox comboBox)
         {
             if (comboBox == null) return;
             
             comboBox.Items.Clear();
             
             // Add "All Categories" option
             comboBox.Items.Add(new ComboBoxItem
             {
                 Content = "All Categories",
                 Tag = 0
             });
             
             // Add individual categories
             foreach (var category in _allCategories)
             {
                 if (category != null)
                 {
                     var item = new ComboBoxItem
                     {
                         Content = category.Name,
                         Tag = category.Id
                     };
                     comboBox.Items.Add(item);
                 }
             }
             
             // Select "All Categories" by default
             comboBox.SelectedIndex = 0;
         }

                 private void LoadAllStockItems()
         {
             try
             {
                 // Load all products and components
                 _allProducts = _databaseContext.GetAllProducts().Where(p => p.IsActive).ToList();
                 _allComponents = _databaseContext.GetAllComponents().Where(c => c.IsActive).ToList();
                 
                 // Ensure we have the required data
                 if (_allProducts == null || _allComponents == null || _allLocations == null)
                 {
                     ErrorDialog.ShowError("Required data not loaded. Please try again.", "Data Loading Error");
                     return;
                 }

                _allStockItems = new List<StockLevelItem>();

                // Load products
                foreach (var product in _allProducts.Where(p => p.IsActive))
                {
                    foreach (var location in _allLocations.Where(l => l.IsActive))
                    {
                        var currentStock = _databaseContext.GetProductStock(product.Id, location.Id);
                        var minimumStock = _databaseContext.GetMinimumStock("Product", product.Id, location.Id);
                        var fullStock = _databaseContext.GetFullStock("Product", product.Id, location.Id);

                        _allStockItems.Add(new StockLevelItem
                        {
                            Id = product.Id,
                            Name = product.Name,
                            SKU = product.SKU,
                            ItemType = "Product",
                            LocationId = location.Id,
                            LocationName = location.Name,
                            CurrentStock = currentStock,
                            MinimumStock = minimumStock,
                            FullStock = fullStock
                        });
                    }
                }

                // Load components
                foreach (var component in _allComponents.Where(c => c.IsActive))
                {
                    foreach (var location in _allLocations.Where(l => l.IsActive))
                    {
                        var currentStock = _databaseContext.GetComponentStock(component.Id, location.Id);
                        var minimumStock = _databaseContext.GetMinimumStock("Component", component.Id, location.Id);
                        var fullStock = _databaseContext.GetFullStock("Component", component.Id, location.Id);

                        _allStockItems.Add(new StockLevelItem
                        {
                            Id = component.Id,
                            Name = component.Name,
                            SKU = component.SKU,
                            ItemType = "Component",
                            LocationId = location.Id,
                            LocationName = location.Name,
                            CurrentStock = currentStock,
                            MinimumStock = minimumStock,
                            FullStock = fullStock
                        });
                    }
                }

                FilterAndUpdateGrid();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in SetStockLevelsWindow.LoadAllStockItems(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

        private void UpdateItemCount()
        {
            var count = dgStockItems.ItemsSource?.Cast<object>().Count() ?? 0;
            txtItemCount.Text = $"({count} items)";
        }

        #region Event Handlers

        

                 private void cmbLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
         {
             if (cmbLocation.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
             {
                 var locationId = selectedItem.Tag as int? ?? 0;
                 
                 // Handle "All Locations" (locationId = 0)
                 if (locationId == 0)
                 {
                     _selectedLocation = null;
                 }
                 else
                 {
                     _selectedLocation = _allLocations?.FirstOrDefault(l => l.Id == locationId);
                 }
                 
                 // Only filter if data is loaded
                 if (_allStockItems != null)
                 {
                     FilterAndUpdateGrid();
                 }
             }
         }

                 private void cmbItemType_SelectionChanged(object sender, SelectionChangedEventArgs e)
         {
             if (cmbItemType.SelectedItem is ComboBoxItem selectedItem)
             {
                 _selectedItemType = selectedItem.Content.ToString();
                 
                 // Only filter if data is loaded
                 if (_allStockItems != null)
                 {
                     FilterAndUpdateGrid();
                 }
             }
         }

         private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
         {
             if (cmbCategory.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
             {
                 var categoryId = selectedItem.Tag as int? ?? 0;
                 
                 // Handle "All Categories" (categoryId = 0)
                 if (categoryId == 0)
                 {
                     _selectedCategory = null;
                 }
                 else
                 {
                     _selectedCategory = _allCategories?.FirstOrDefault(c => c.Id == categoryId);
                 }
                 
                 // Only filter if data is loaded
                 if (_allStockItems != null)
                 {
                     FilterAndUpdateGrid();
                 }
             }
         }

        private void txtNewMinStock_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow numbers
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void txtNewFullStock_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow numbers
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

                 private void btnSaveStock_Click(object sender, RoutedEventArgs e)
         {
             try
             {
                 if (_selectedStockItem == null)
                 {
                     ErrorDialog.ShowError("Please select an item from the grid.", "Validation Error");
                     return;
                 }

                 var locationId = _selectedStockItem.LocationId;
                 var itemId = _selectedStockItem.Id;
                 var itemType = _selectedStockItem.ItemType;

                // Validate minimum stock
                if (!int.TryParse(txtNewMinStock.Text, out int newMinStock))
                {
                    ErrorDialog.ShowError("Please enter a valid number for minimum stock.", "Validation Error");
                    return;
                }

                if (newMinStock < 0)
                {
                    ErrorDialog.ShowError("Minimum stock cannot be negative.", "Validation Error");
                    return;
                }

                // Validate full stock
                if (!int.TryParse(txtNewFullStock.Text, out int newFullStock))
                {
                    ErrorDialog.ShowError("Please enter a valid number for full stock.", "Validation Error");
                    return;
                }

                if (newFullStock < 0)
                {
                    ErrorDialog.ShowError("Full stock cannot be negative.", "Validation Error");
                    return;
                }

                // Set the stock levels
                _databaseContext.SetMinimumStock(itemType, itemId, locationId, newMinStock);
                _databaseContext.SetFullStock(itemType, itemId, locationId, newFullStock);

                string message = $"Stock levels updated successfully!\n\nItem: {txtItemName.Text}\nLocation: {_selectedLocation?.Name}\nNew Min Stock: {newMinStock}\nNew Full Stock: {newFullStock}";

                ErrorDialog.ShowSuccess(message, "Success");
                
                                 // Refresh the data
                 LoadAllStockItems();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving stock levels: {ex.Message}", "Error");
            }
        }

        private void dgStockItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedStockItem = dgStockItems.SelectedItem as StockLevelItem;
            
            if (_selectedStockItem != null)
            {
                // Only update the min and full stock text fields
                txtNewMinStock.Text = _selectedStockItem.MinimumStock.ToString();
                txtNewFullStock.Text = _selectedStockItem.FullStock.ToString();
            }
        }

        #endregion

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

                 private void FilterAndUpdateGrid()
         {
             try
             {
                 // Ensure _allStockItems is initialized
                 if (_allStockItems == null)
                 {
                     _allStockItems = new List<StockLevelItem>();
                 }

                 var filteredItems = _allStockItems.AsEnumerable();

                 // Filter by location if selected
                 if (_selectedLocation != null)
                 {
                     filteredItems = filteredItems.Where(item => item.LocationId == _selectedLocation.Id);
                 }

                 // Filter by item type if selected
                 if (!string.IsNullOrEmpty(_selectedItemType) && _selectedItemType != "Product & Component")
                 {
                     filteredItems = filteredItems.Where(item => item.ItemType == _selectedItemType);
                 }

                 // Filter by category if selected
                 if (_selectedCategory != null)
                 {
                     filteredItems = filteredItems.Where(item => 
                     {
                         if (item.ItemType == "Product")
                         {
                             var product = _allProducts?.FirstOrDefault(p => p.Id == item.Id);
                             return product?.CategoryId == _selectedCategory.Id;
                         }
                         else if (item.ItemType == "Component")
                         {
                             var component = _allComponents?.FirstOrDefault(c => c.Id == item.Id);
                             return component?.CategoryId == _selectedCategory.Id;
                         }
                         return false;
                     });
                 }

                 var filteredList = filteredItems.ToList();
                 dgStockItems.ItemsSource = filteredList;
                 UpdateItemCount();
             }
             catch (Exception ex)
             {
                 ErrorDialog.ShowError($"Error filtering grid: {ex.Message}", "Error");
             }
         }

        


    }

    public class StockLevelItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SKU { get; set; }
        public string ItemType { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public int FullStock { get; set; }
        
        public bool IsLowStock => CurrentStock <= MinimumStock && MinimumStock > 0;
        
        public string StockStatus
        {
            get
            {
                if (CurrentStock == 0)
                    return "Out of Stock";
                else if (IsLowStock)
                    return "Low Stock";
                else
                    return "Well Stocked";
            }
        }
    }
} 