using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class EditCategoryWindow : Window
    {
        private Category _category;
        private List<Category> _allCategories;

        public EditCategoryWindow(Category category)
        {
            InitializeComponent();
            _category = category;
            LoadData();
            LoadCategoryData();
        }

        private void LoadData()
        {
            try
            {
                LoadParentCategories();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading parent categories: {ex.Message}", "Error");
            }
        }

        private void LoadParentCategories()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using var dbContext = new DatabaseContext(connectionString);
                
                var categories = dbContext.GetAllCategories();
                cmbParentCategory.Items.Clear();
                cmbParentCategory.Items.Insert(0, new ComboBoxItem { Content = "No Parent (Top Level)" });
                
                foreach (var category in categories.Where(c => c.IsActive && c.Id != _category.Id))
                {
                    cmbParentCategory.Items.Add(new ComboBoxItem 
                    { 
                        Content = category.Name, 
                        Tag = category.Id 
                    });
                }
                
                cmbParentCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading parent categories: {ex.Message}", "Database Error");
            }
        }

        private void LoadCategoryData()
        {
            txtName.Text = _category.Name;
            txtDescription.Text = _category.Description ?? "";
            txtSortOrder.Text = _category.SortOrder.ToString();

            if (_category.ParentId.HasValue)
            {
                var parentCategory = _allCategories.FirstOrDefault(c => c.Id == _category.ParentId.Value);
                if (parentCategory != null)
                {
                    cmbParentCategory.SelectedItem = parentCategory;
                }
            }
            else
            {
                cmbParentCategory.SelectedIndex = 0; // No parent
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                UpdateCategory();
                ErrorDialog.ShowSuccess("Category updated successfully!", "Success");
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error updating category: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ErrorDialog.ShowWarning("Category name is required.", "Validation Error");
                return false;
            }

            if (!int.TryParse(txtSortOrder.Text, out int sortOrder))
            {
                ErrorDialog.ShowWarning("Sort order must be a number.", "Validation Error");
                return false;
            }

            return true;
        }

        private void UpdateCategory()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using var dbContext = new DatabaseContext(connectionString);
                
                var sql = @"
                    UPDATE Categories 
                    SET Name = @name, Description = @description, IsActive = @isActive, LastModified = @lastModified 
                    WHERE Id = @id";

                using var command = dbContext.CreateCommand(sql);
                command.Parameters.AddWithValue("@name", txtName.Text.Trim());
                command.Parameters.AddWithValue("@description", txtDescription.Text.Trim());
                command.Parameters.AddWithValue("@isActive", chkIsActive.IsChecked ?? true);
                command.Parameters.AddWithValue("@lastModified", DateTime.Now);
                command.Parameters.AddWithValue("@id", _category.Id);
                
                command.ExecuteNonQuery();
                
                ErrorDialog.ShowSuccess("Category updated successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error updating category: {ex.Message}", "Database Error");
            }
        }

        private int? GetSelectedParentId()
        {
            if (cmbParentCategory.SelectedItem is ComboBoxItem && cmbParentCategory.SelectedIndex == 0)
            {
                return null; // No parent selected
            }

            if (cmbParentCategory.SelectedItem is Category selectedCategory)
            {
                return selectedCategory.Id;
            }

            return null;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtSortOrder_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers
            var regex = new System.Text.RegularExpressions.Regex(@"^\d*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void txtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Temporarily disabled
        }

        private void txtDescription_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Temporarily disabled
        }

        private void cmbParentCategory_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Temporarily disabled
        }

        private void txtSortOrder_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Temporarily disabled
        }

        private void chkIsActive_Changed(object sender, RoutedEventArgs e)
        {
            // Temporarily disabled
        }
    }
} 