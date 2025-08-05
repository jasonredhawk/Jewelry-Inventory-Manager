using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Views;
using Moonglow_DB.Data;
using Moonglow_DB.Models;

namespace Moonglow_DB.Views
{
    public partial class ProductsWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<Product> _allProducts;
        private List<Product> _filteredProducts;
        private Product _selectedProduct;

        public ProductsWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                _allProducts = _databaseContext.GetAllProducts();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading products: {ex.Message}", "Error");
                _allProducts = new List<Product>();
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            if (_allProducts == null) return;

            var searchText = txtSearch.Text?.ToLower() ?? "";
            
            _filteredProducts = _allProducts.Where(product =>
                string.IsNullOrEmpty(searchText) ||
                product.Name.ToLower().Contains(searchText) ||
                product.SKU.ToLower().Contains(searchText) ||
                (product.Description?.ToLower().Contains(searchText) ?? false)
            ).ToList();

            dgProducts.ItemsSource = _filteredProducts;
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = _selectedProduct != null;
            btnEditProduct.IsEnabled = hasSelection;
            btnDeleteProduct.IsEnabled = hasSelection;
        }

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addProductWindow = new AddProductWindow(_databaseContext);
                if (addProductWindow.ShowDialog() == true)
                {
                    LoadProducts();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening add product window: {ex.Message}", "Error");
            }
        }

        private void btnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedProduct != null)
                {
                    var editProductWindow = new EditProductWindow(_databaseContext, _selectedProduct);
                    if (editProductWindow.ShowDialog() == true)
                    {
                        LoadProducts();
                    }
                }
                else
                {
                    ErrorDialog.ShowInformation("Please select a product to edit.", "No Selection");
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening edit product window: {ex.Message}", "Error");
            }
        }

        private void btnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedProduct != null)
                {
                    var result = ErrorDialog.ShowConfirmation($"Are you sure you want to delete the product '{_selectedProduct.Name}'?", "Confirm Delete");
                    if (result == true)
                    {
                        // Delete the product
                        var sql = "DELETE FROM Products WHERE Id = @Id";
                        using var command = _databaseContext.CreateCommand(sql);
                        command.Parameters.AddWithValue("@Id", _selectedProduct.Id);
                        command.ExecuteNonQuery();
                        
                        LoadProducts();
                        ErrorDialog.ShowSuccess("Product deleted successfully!", "Success");
                    }
                }
                else
                {
                    ErrorDialog.ShowInformation("Please select a product to delete.", "No Selection");
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error deleting product: {ex.Message}", "Error");
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void dgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedProduct = dgProducts.SelectedItem as Product;
            UpdateButtonStates();
        }
    }
} 