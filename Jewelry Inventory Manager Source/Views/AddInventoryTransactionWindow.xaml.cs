using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;

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
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void LoadTransactionTypes()
        {
            cmbTransactionType.Items.Clear();
            cmbTransactionType.Items.Add(new ComboBoxItem { Content = "Purchase" });
            cmbTransactionType.Items.Add(new ComboBoxItem { Content = "Sale" });
            cmbTransactionType.Items.Add(new ComboBoxItem { Content = "Adjustment" });
            cmbTransactionType.Items.Add(new ComboBoxItem { Content = "Return" });
        }

        private void LoadItemTypes()
        {
            cmbItemType.Items.Clear();
            cmbItemType.Items.Add(new ComboBoxItem { Content = "Product" });
            cmbItemType.Items.Add(new ComboBoxItem { Content = "Component" });
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
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading locations: {ex.Message}", "Database Error");
            }
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

        private void cmbTransactionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Temporarily disabled - lblQuantity control doesn't exist
            /*
            if (cmbTransactionType.SelectedItem is ComboBoxItem selectedItem)
            {
                var transactionType = selectedItem.Content.ToString();
                switch (transactionType)
                {
                    case "Purchase":
                        lblQuantity.Content = "Quantity Purchased:";
                        break;
                    case "Sale":
                        lblQuantity.Content = "Quantity Sold:";
                        break;
                    case "Adjustment":
                        lblQuantity.Content = "Adjustment Quantity:";
                        break;
                    case "Return":
                        lblQuantity.Content = "Quantity Returned:";
                        break;
                }
            }
            */
        }

        private void cmbItemType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbItem.ItemsSource = null;
            _selectedItem = null;

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
            if (cmbItem.SelectedItem != null)
            {
                if (cmbItem.SelectedItem is Product product)
                {
                    _selectedItem = new InventoryItem
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Type = "Product",
                        CurrentStock = product.CurrentStock,
                        MinimumStock = product.MinimumStock
                    };
                }
                else if (cmbItem.SelectedItem is Component component)
                {
                    _selectedItem = new InventoryItem
                    {
                        Id = component.Id,
                        Name = component.Name,
                        Type = "Component",
                        CurrentStock = component.CurrentStock,
                        MinimumStock = component.MinimumStock
                    };
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

            if (cmbItem.SelectedItem == null)
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
                ErrorDialog.ShowWarning("Please enter a valid quantity.", "Validation Error");
                return false;
            }

            return true;
        }

        private void SaveTransaction()
        {
            try
            {
                var transactionType = cmbTransactionType.SelectedItem?.ToString() ?? "Adjustment";
                var quantity = int.Parse(txtQuantity.Text);
                
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
                
                ErrorDialog.ShowSuccess("Transaction saved successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving transaction: {ex.Message}", "Database Error");
            }
        }

        private TransactionType GetTransactionType(string transactionType)
        {
            return transactionType switch
            {
                "Purchase" => TransactionType.Purchase,
                "Sale" => TransactionType.Sale,
                "Adjustment" => TransactionType.Adjustment,
                "Return" => TransactionType.Return,
                _ => TransactionType.Adjustment
            };
        }

        private int GetTransactionQuantity(string transactionType, int quantity)
        {
            return transactionType switch
            {
                "Sale" => -quantity, // Negative for sales
                "Return" => quantity, // Positive for returns
                _ => quantity // Positive for purchases and adjustments
            };
        }

        private string GetSelectedItemType()
        {
            if (cmbItemType.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content?.ToString() ?? "Component";
            }
            return "Component";
        }

        private int GetSelectedItemId()
        {
            if (cmbItem.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag as int? ?? 0;
            }
            return 0;
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

        private void UpdateQuantityLabel()
        {
            // Temporarily disabled - lblQuantity control doesn't exist
            /*
            if (lblQuantity != null)
            {
                lblQuantity.Content = $"Quantity: {txtQuantity.Text}";
            }
            */
        }

        private void txtQuantity_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Temporarily disabled - lblQuantity control doesn't exist
            /*
            UpdateQuantityLabel();
            */
        }

        private void txtQuantity_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Temporarily disabled - lblQuantity control doesn't exist
            /*
            e.Handled = !int.TryParse(e.Text, out _);
            */
        }

        private void txtQuantity_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Temporarily disabled - lblQuantity control doesn't exist
            /*
            if (e.Key == System.Windows.Input.Key.Space)
            {
                e.Handled = true;
            }
            */
        }
    }
} 