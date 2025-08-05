using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class AddCategoryWindow : Window
    {
        private List<Category> _allCategories;

        public AddCategoryWindow()
        {
            InitializeComponent();
            LoadData();
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
                
                foreach (var category in categories.Where(c => c.IsActive))
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

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                SaveCategory();
                ErrorDialog.ShowSuccess("Category created successfully!", "Success");
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error creating category: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ErrorDialog.ShowWarning("Category name is required.", "Validation Error");
                return false;
            }

            return true;
        }

        private void SaveCategory()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using var dbContext = new DatabaseContext(connectionString);
                
                var sql = @"
                    INSERT INTO Categories (Name, Description, IsActive, CreatedDate, LastModified) 
                    VALUES (@name, @description, @isActive, @createdDate, @lastModified)";

                using var command = dbContext.CreateCommand(sql);
                command.Parameters.AddWithValue("@name", txtName.Text.Trim());
                command.Parameters.AddWithValue("@description", txtDescription.Text.Trim());
                command.Parameters.AddWithValue("@isActive", chkIsActive.IsChecked ?? true);
                command.Parameters.AddWithValue("@createdDate", DateTime.Now);
                command.Parameters.AddWithValue("@lastModified", DateTime.Now);
                
                command.ExecuteNonQuery();
                
                ErrorDialog.ShowSuccess("Category saved successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving category: {ex.Message}", "Database Error");
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
            // Only allow numeric input
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void txtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSummary();
        }

        private void cmbParentCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var summary = $"Name: {txtName.Text}\n";
            summary += $"Description: {txtDescription.Text}\n";
            
            if (cmbParentCategory.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                summary += $"Parent Category: {selectedItem.Content}\n";
            }
            else
            {
                summary += "Parent Category: None (Top Level)\n";
            }
            
            summary += $"Sort Order: {txtSortOrder.Text}\n";
            summary += $"Active: {(chkIsActive.IsChecked ?? false ? "Yes" : "No")}";
            
            txtSummary.Text = summary;
        }
    }
} 