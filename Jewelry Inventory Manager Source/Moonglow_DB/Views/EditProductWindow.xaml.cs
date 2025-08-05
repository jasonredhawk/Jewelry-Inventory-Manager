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
    public partial class EditProductWindow : Window
    {
        private Product _product;
        private List<Component> _allComponents;
        private List<Category> _allCategories;
        private List<ProductComponent> _selectedComponents;
        private Component _selectedComponent;
        private readonly DatabaseContext _databaseContext;

        public EditProductWindow(DatabaseContext databaseContext, Product product)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _product = product;
            _selectedComponents = new List<ProductComponent>();
            
            LoadData();
            LoadProductData();
        }

        private void LoadData()
        {
            try
            {
                InitializeFilteredComboBox();
                LoadCategories();
                UpdateComponentList();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void InitializeFilteredComboBox()
        {
            try
            {
                // Load all components
                _allComponents = _databaseContext.GetAllComponents().Where(c => c.IsActive).ToList();
                
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
            _allCategories = _databaseContext.GetAllCategories();
            cmbCategory.ItemsSource = _allCategories;
        }

        private void LoadProductData()
        {
            txtSKU.Text = _product.SKU;
            txtName.Text = _product.Name;
            txtDescription.Text = _product.Description ?? "";
            txtPrice.Text = _product.Price.ToString();

            if (_product.CategoryId.HasValue)
            {
                var category = _allCategories.FirstOrDefault(c => c.Id == _product.CategoryId.Value);
                if (category != null)
                {
                    cmbCategory.SelectedItem = category;
                }
            }

            LoadProductComponents();
        }

        private void LoadProductComponents()
        {
            var sql = @"
                SELECT pc.ComponentId, pc.Quantity, c.Name as ComponentName
                FROM ProductComponents pc
                JOIN Components c ON pc.ComponentId = c.Id
                WHERE pc.ProductId = @ProductId";

            using (var command = _databaseContext.CreateCommand(sql))
            {
                command.Parameters.AddWithValue("@ProductId", _product.Id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var productComponent = new ProductComponent
                        {
                            ComponentId = reader.GetInt32(0),
                            Quantity = reader.GetInt32(1),
                            ComponentName = reader.GetString(2)
                        };
                        _selectedComponents.Add(productComponent);
                    }
                }
            }
            UpdateComponentList();
        }

        private void UpdateComponentList()
        {
            dgComponents.ItemsSource = null;
            dgComponents.ItemsSource = _selectedComponents;
        }

        private void FilteredComponentComboBox_SelectionChanged(object sender, object selectedItem)
        {
            if (selectedItem is ComboBoxDisplayItem displayItem && displayItem.Item is Component component)
            {
                _selectedComponent = component;
            }
            else
            {
                _selectedComponent = null;
            }
        }

        private void btnAddComponent_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedComponent == null)
            {
                ErrorDialog.ShowWarning("Please select a component.", "No Selection");
                return;
            }

            if (!int.TryParse(txtComponentQuantity.Text, out int quantity) || quantity <= 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid quantity.", "Invalid Quantity");
                return;
            }

            var existingComponent = _selectedComponents.FirstOrDefault(c => c.ComponentId == _selectedComponent.Id);

            if (existingComponent != null)
            {
                ErrorDialog.ShowWarning("This component is already added to the product.", "Duplicate Component");
                return;
            }

            var productComponent = new ProductComponent
            {
                ComponentId = _selectedComponent.Id,
                ComponentName = _selectedComponent.Name,
                Quantity = quantity
            };

            _selectedComponents.Add(productComponent);
            UpdateComponentList();
            ClearComponentForm();
        }

        private void btnRemoveComponent_Click(object sender, RoutedEventArgs e)
        {
            if (dgComponents.SelectedItem is ProductComponent selectedComponent)
            {
                _selectedComponents.Remove(selectedComponent);
                UpdateComponentList();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                UpdateProduct();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving product: {ex.Message}", "Save Error");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtSKU.Text))
            {
                ErrorDialog.ShowWarning("Please enter a SKU.", "Validation Error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ErrorDialog.ShowWarning("Please enter a product name.", "Validation Error");
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid price.", "Validation Error");
                return false;
            }

            return true;
        }

        private void UpdateProduct()
        {
            // Update product
            var updateProductSql = @"
                UPDATE Products 
                SET Name = @Name, Description = @Description, Price = @Price, 
                    CategoryId = @CategoryId, LastModified = @LastModified
                WHERE Id = @Id";

            using (var command = _databaseContext.CreateCommand(updateProductSql))
            {
                command.Parameters.AddWithValue("@Name", txtName.Text.Trim());
                command.Parameters.AddWithValue("@Description", txtDescription.Text.Trim());
                command.Parameters.AddWithValue("@Price", decimal.Parse(txtPrice.Text));
                command.Parameters.AddWithValue("@CategoryId", cmbCategory.SelectedItem != null ? 
                    (object)((Category)cmbCategory.SelectedItem).Id : DBNull.Value);
                command.Parameters.AddWithValue("@LastModified", DateTime.Now);
                command.Parameters.AddWithValue("@Id", _product.Id);

                command.ExecuteNonQuery();
            }

            // Delete existing product components
            var deleteComponentsSql = "DELETE FROM ProductComponents WHERE ProductId = @ProductId";
            using (var command = _databaseContext.CreateCommand(deleteComponentsSql))
            {
                command.Parameters.AddWithValue("@ProductId", _product.Id);
                command.ExecuteNonQuery();
            }

            // Insert new product components
            var insertComponentSql = @"
                INSERT INTO ProductComponents (ProductId, ComponentId, Quantity) 
                VALUES (@ProductId, @ComponentId, @Quantity)";

            foreach (var component in _selectedComponents)
            {
                using (var command = _databaseContext.CreateCommand(insertComponentSql))
                {
                    command.Parameters.AddWithValue("@ProductId", _product.Id);
                    command.Parameters.AddWithValue("@ComponentId", component.ComponentId);
                    command.Parameters.AddWithValue("@Quantity", component.Quantity);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void ClearComponentForm()
        {
            filteredComponentComboBox.ClearSelection();
            _selectedComponent = null;
            txtComponentQuantity.Text = "1";
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtPrice_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow decimal input
            e.Handled = !decimal.TryParse(e.Text, out _);
        }

        private void txtComponentQuantity_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numeric input
            e.Handled = !int.TryParse(e.Text, out _);
        }
    }

    public class ProductComponent
    {
        public int ComponentId { get; set; }
        public string ComponentName { get; set; }
        public int Quantity { get; set; }
    }
} 