using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using Moonglow_DB.Views.Controls;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class AddProductWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<Component> _allComponents;
        private List<ProductComponentSelection> _selectedComponents;
        private Component _selectedComponent;

        public AddProductWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _selectedComponents = new List<ProductComponentSelection>();
            
            // Set default values
            txtPrice.Text = "0.00";
            
            InitializeFilteredComboBox();
            LoadCategories();
            
            // Delay UpdateSummary to ensure controls are initialized
            Dispatcher.BeginInvoke(new Action(() => UpdateSummary()));
        }

        private void InitializeFilteredComboBox()
        {
            try
            {
                // Load all components
                _allComponents = GetAllComponents().Where(c => c.IsActive).ToList();
                
                // Create filter service
                var filterService = new ItemFilterService(_databaseContext);
                
                // Initialize filtered combo box
                filteredComponentComboBox.Initialize(filterService, new List<Product>(), _allComponents);
                
                // Set to components only (false)
                filteredComponentComboBox.SetItemType(false);
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error initializing filtered combo box: {ex.Message}", "Error");
            }
        }

        private void LoadCategories()
        {
            try
            {
                var categories = _databaseContext.GetAllCategories();
                cmbCategory.ItemsSource = categories;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading categories: {ex.Message}", "Error");
            }
        }

        private List<Component> GetAllComponents()
        {
            var components = new List<Component>();
            
            using var connection = _databaseContext.GetConnection();
            var sql = @"
                SELECT Id, SKU, Name, Description, Cost, IsActive, LastModified 
                FROM Components 
                WHERE IsActive = 1 
                ORDER BY Name";
            
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                components.Add(new Component
                {
                    Id = reader.GetInt32(0),
                    SKU = reader.GetString(1),
                    Name = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Cost = reader.GetDecimal(4),
                    IsActive = reader.GetBoolean(5),
                    LastModified = reader.GetDateTime(6)
                });
            }
            
            return components;
        }

        private void UpdateSummary()
        {
            if (_selectedComponents.Count == 0)
            {
                if (txtSummary != null)
                    txtSummary.Text = "Add components to see summary";
                if (txtSKU != null)
                    txtSKU.Text = "";
                return;
            }

            // Generate combined SKU
            var componentSKUs = new List<string>();
            foreach (var selection in _selectedComponents)
            {
                for (int i = 0; i < selection.Quantity; i++)
                {
                    componentSKUs.Add(selection.Component.SKU);
                }
            }
            var combinedSKU = string.Join(" + ", componentSKUs);
            
            if (txtSKU != null)
                txtSKU.Text = combinedSKU;

            // Calculate total cost
            var totalCost = _selectedComponents.Sum(sc => sc.TotalCost);
            
            // Update summary
            if (txtSummary != null)
            {
                txtSummary.Text = $"Total Components: {_selectedComponents.Count}\n" +
                                 $"Total Cost: {totalCost:C}\n" +
                                 $"Combined SKU: {combinedSKU}";
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            if (_selectedComponents.Count == 0)
            {
                ErrorDialog.ShowWarning("Please add at least one component to the product.", "Validation Error");
                return;
            }

            try
            {
                var selectedCategory = cmbCategory.SelectedItem as Category;
                var product = new Product
                {
                    SKU = txtSKU.Text.Trim(),
                    Name = txtName.Text.Trim(),
                    Description = txtDescription.Text.Trim(),
                    CategoryId = selectedCategory?.Id,
                    Price = decimal.Parse(txtPrice.Text),
                    IsActive = chkIsActive.IsChecked ?? true,
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now
                };

                SaveProductWithComponents(product);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving product: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtSKU.Text))
            {
                ErrorDialog.ShowWarning("Please enter a SKU.", "Validation Error");
                txtSKU.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ErrorDialog.ShowWarning("Please enter a product name.", "Validation Error");
                txtName.Focus();
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text, out _))
            {
                ErrorDialog.ShowWarning("Please enter a valid price.", "Validation Error");
                txtPrice.Focus();
                return false;
            }



            return true;
        }

        private void SaveProductWithComponents(Product product)
        {
            using var connection = _databaseContext.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Insert the product
                var productSql = @"
                    INSERT INTO Products (SKU, Name, Description, CategoryId, Price, IsActive, CreatedDate, LastModified)
                    VALUES (@SKU, @Name, @Description, @CategoryId, @Price, @IsActive, @CreatedDate, @LastModified);
                    SELECT LAST_INSERT_ID();";

                using var productCommand = new MySqlCommand(productSql, connection);
                productCommand.Transaction = transaction;
                productCommand.Parameters.AddWithValue("@SKU", product.SKU);
                productCommand.Parameters.AddWithValue("@Name", product.Name);
                productCommand.Parameters.AddWithValue("@Description", product.Description);
                productCommand.Parameters.AddWithValue("@CategoryId", (object)product.CategoryId ?? DBNull.Value);
                productCommand.Parameters.AddWithValue("@Price", product.Price);

                productCommand.Parameters.AddWithValue("@IsActive", product.IsActive);
                productCommand.Parameters.AddWithValue("@CreatedDate", product.CreatedDate);
                productCommand.Parameters.AddWithValue("@LastModified", product.LastModified);

                var productId = Convert.ToInt32(productCommand.ExecuteScalar());

                // Insert product components
                var componentSql = @"
                    INSERT INTO ProductComponents (ProductId, ComponentId, Quantity)
                    VALUES (@ProductId, @ComponentId, @Quantity)";

                foreach (var selection in _selectedComponents)
                {
                    using var componentCommand = new MySqlCommand(componentSql, connection);
                    componentCommand.Transaction = transaction;
                    componentCommand.Parameters.AddWithValue("@ProductId", productId);
                    componentCommand.Parameters.AddWithValue("@ComponentId", selection.Component.Id);
                    componentCommand.Parameters.AddWithValue("@Quantity", selection.Quantity);
                    componentCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void btnAddComponent_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedComponent == null)
            {
                ErrorDialog.ShowWarning("Please select a component.", "Validation Error");
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid quantity.", "Validation Error");
                return;
            }

            // Check if component is already added
            var existingSelection = _selectedComponents.FirstOrDefault(sc => sc.Component.Id == _selectedComponent.Id);
            if (existingSelection != null)
            {
                existingSelection.Quantity += quantity;
            }
            else
            {
                _selectedComponents.Add(new ProductComponentSelection
                {
                    Component = _selectedComponent,
                    Quantity = quantity
                });
            }

            dgSelectedComponents.ItemsSource = null;
            dgSelectedComponents.ItemsSource = _selectedComponents;
            UpdateSummary();

            // Clear selection
            filteredComponentComboBox.ClearSelection();
            _selectedComponent = null;
            txtQuantity.Text = "1";
        }

        private void btnRemoveComponent_Click(object sender, RoutedEventArgs e)
        {
            if (dgSelectedComponents.SelectedItem is ProductComponentSelection selection)
            {
                _selectedComponents.Remove(selection);
                dgSelectedComponents.ItemsSource = null;
                dgSelectedComponents.ItemsSource = _selectedComponents;
                UpdateSummary();
            }
        }

        private void FilteredComponentComboBox_SelectionChanged(object sender, object selectedItem)
        {
            if (selectedItem is ComboBoxDisplayItem displayItem && displayItem.Item is Component component)
            {
                _selectedComponent = component;
                txtQuantity.Text = "1";
            }
            else
            {
                _selectedComponent = null;
            }
        }

        private void txtQuantity_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void chkAutoSKU_Checked(object sender, RoutedEventArgs e)
        {
            if (txtSKU != null)
            {
                txtSKU.IsEnabled = false;
                UpdateSummary();
            }
        }

        private void chkAutoSKU_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtSKU != null)
            {
                txtSKU.IsEnabled = true;
                txtSKU.Text = "";
            }
        }
    }
} 