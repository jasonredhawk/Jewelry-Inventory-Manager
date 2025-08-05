using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class CategoriesWindow : Window
    {
        private List<Category> _allCategories;
        private List<Category> _filteredCategories;
        private Category _selectedCategory;
        private readonly DatabaseContext _databaseContext;

        public CategoriesWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                _allCategories = _databaseContext.GetAllCategories();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading categories: {ex.Message}", "Database Error");
                _allCategories = new List<Category>();
                ApplyFilters();
            }
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = _selectedCategory != null;
            btnEditCategory.IsEnabled = hasSelection;
            btnDeleteCategory.IsEnabled = hasSelection;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allCategories == null) return;

            var searchText = txtSearch.Text.ToLower();
            var filterType = cmbFilter.SelectedItem?.ToString() ?? "All";

            _filteredCategories = _allCategories.Where(category =>
            {
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                   category.Name.ToLower().Contains(searchText) ||
                                   (category.Description?.ToLower().Contains(searchText) ?? false);

                var matchesFilter = filterType switch
                {
                    "Active" => category.IsActive,
                    "Inactive" => !category.IsActive,
                    _ => true // "All"
                };

                return matchesSearch && matchesFilter;
            }).ToList();

            dgCategories.ItemsSource = _filteredCategories;
        }

        private void dgCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCategory = dgCategories.SelectedItem as Category;
            UpdateButtonStates();
        }

        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            var addCategoryWindow = new AddCategoryWindow();
            addCategoryWindow.ShowDialog();
            LoadCategories(); // Refresh the list
        }

        private void btnEditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null)
            {
                ErrorDialog.ShowInformation("Please select a category to edit.", "No Selection");
                return;
            }

            var editCategoryWindow = new EditCategoryWindow(_selectedCategory);
            editCategoryWindow.ShowDialog();
            LoadCategories(); // Refresh the list
        }

        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null)
            {
                ErrorDialog.ShowInformation("Please select a category to delete.", "No Selection");
                return;
            }

            var result = ErrorDialog.ShowConfirmation(
                $"Are you sure you want to delete category '{_selectedCategory.Name}'?\n\nThis will also delete all subcategories.",
                "Confirm Delete");

            if (result == true)
            {
                try
                {
                    DeleteCategory();
                }
                catch (Exception ex)
                {
                    ErrorDialog.ShowError($"Error deleting category: {ex.Message}", "Error");
                }
            }
        }

        private void DeleteCategory()
        {
            try
            {
                var sql = "DELETE FROM Categories WHERE Id = @id";
                using var command = _databaseContext.CreateCommand(sql);
                command.Parameters.AddWithValue("@id", _selectedCategory.Id);
                command.ExecuteNonQuery();
                
                LoadCategories();
                ErrorDialog.ShowSuccess("Category deleted successfully!", "Success");
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error deleting category: {ex.Message}", "Database Error");
            }
        }

        private void DeleteSubcategories(int parentId, DatabaseContext context)
        {
            var sql = "SELECT Id FROM Categories WHERE ParentId = @ParentId";
            using (var command = context.CreateCommand(sql))
            {
                command.Parameters.AddWithValue("@ParentId", parentId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var subcategoryId = reader.GetInt32(0);
                        DeleteSubcategories(subcategoryId, context);
                    }
                }
            }

            // Delete the subcategory
            var deleteSql = "DELETE FROM Categories WHERE Id = @Id";
            using (var deleteCommand = context.CreateCommand(deleteSql))
            {
                deleteCommand.Parameters.AddWithValue("@Id", parentId);
                deleteCommand.ExecuteNonQuery();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCategories();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 