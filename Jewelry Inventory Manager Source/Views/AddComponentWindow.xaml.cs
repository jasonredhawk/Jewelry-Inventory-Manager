using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class AddComponentWindow : Window
    {
        private List<Category> _allCategories;

        public AddComponentWindow()
        {
            InitializeComponent();
            LoadData();
            SetDefaultValues();
        }

        private void LoadData()
        {
            try
            {
                LoadCategories();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading categories: {ex.Message}", "Error");
            }
        }

        private void LoadCategories()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using var dbContext = new DatabaseContext(connectionString);
                
                var categories = dbContext.GetAllCategories();
                cmbCategory.Items.Clear();
                cmbCategory.Items.Add(new ComboBoxItem { Content = "No Category", Tag = (int?)null });
                
                foreach (var category in categories.Where(c => c.IsActive))
                {
                    cmbCategory.Items.Add(new ComboBoxItem 
                    { 
                        Content = category.Name, 
                        Tag = category.Id 
                    });
                }
                
                cmbCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading categories: {ex.Message}", "Database Error");
            }
        }

        private void SetDefaultValues()
        {
            txtCost.Text = "0";
            txtDescription.Text = string.Empty;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                SaveComponent();
                ErrorDialog.ShowSuccess("Component added successfully!", "Success");
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error adding component: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtSKU.Text))
            {
                ErrorDialog.ShowWarning("Component SKU is required.", "Validation Error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ErrorDialog.ShowWarning("Component name is required.", "Validation Error");
                return false;
            }

            if (!decimal.TryParse(txtCost.Text, out decimal cost) || cost < 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid cost.", "Validation Error");
                return false;
            }

            return true;
        }

        private void SaveComponent()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using var dbContext = new DatabaseContext(connectionString);
                
                var sql = @"
                    INSERT INTO Components (SKU, Name, Description, CategoryId, Price, Cost, IsActive, CreatedDate, LastModified) 
                    VALUES (@sku, @name, @description, @categoryId, @price, @cost, @isActive, @createdDate, @lastModified)";

                using var command = dbContext.CreateCommand(sql);
                command.Parameters.AddWithValue("@sku", txtSKU.Text.Trim());
                command.Parameters.AddWithValue("@name", txtName.Text.Trim());
                command.Parameters.AddWithValue("@description", txtDescription.Text.Trim());
                command.Parameters.AddWithValue("@categoryId", GetSelectedCategoryId() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@price", decimal.Parse(txtCost.Text));
                command.Parameters.AddWithValue("@cost", decimal.Parse(txtCost.Text));
                command.Parameters.AddWithValue("@isActive", chkIsActive.IsChecked ?? true);
                command.Parameters.AddWithValue("@createdDate", DateTime.Now);
                command.Parameters.AddWithValue("@lastModified", DateTime.Now);
                
                command.ExecuteNonQuery();
                
                ErrorDialog.ShowSuccess("Component saved successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving component: {ex.Message}", "Database Error");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtCost_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers and decimal point
            var regex = new System.Text.RegularExpressions.Regex(@"^\d*\.?\d*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private int? GetSelectedCategoryId()
        {
            if (cmbCategory.SelectedItem is Category selectedCategory)
            {
                return selectedCategory.Id;
            }
            return null;
        }

        private void chkAutoSKU_Checked(object sender, RoutedEventArgs e)
        {
            // Auto-generate SKU logic
            if (string.IsNullOrWhiteSpace(txtName.Text))
                return;
                
            var sku = txtName.Text.Replace(" ", "").ToUpper();
            txtSKU.Text = sku;
        }

        private void chkAutoSKU_Unchecked(object sender, RoutedEventArgs e)
        {
            // Clear SKU when auto-generation is disabled
            txtSKU.Text = string.Empty;
        }
    }
} 