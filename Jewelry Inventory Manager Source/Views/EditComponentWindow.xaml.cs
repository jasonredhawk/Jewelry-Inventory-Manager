using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class EditComponentWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private readonly Component _component;
        private List<Category> _categories;
        
        public EditComponentWindow(DatabaseContext databaseContext, Component component)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _component = component;
            LoadCategories();
            LoadComponentData();
        }
        
        private void LoadCategories()
        {
            try
            {
                _categories = _databaseContext.GetAllCategories();
                cmbCategory.ItemsSource = _categories;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading categories: {ex.Message}", "Error");
            }
        }
        
        private void LoadComponentData()
        {
            txtSKU.Text = _component.SKU;
            txtName.Text = _component.Name;
            txtDescription.Text = _component.Description ?? "";
            txtCost.Text = _component.Cost.ToString("F2");
            chkIsActive.IsChecked = _component.IsActive;
            
            // Set category if exists
            if (_component.CategoryId.HasValue)
            {
                var category = _categories?.FirstOrDefault(c => c.Id == _component.CategoryId.Value);
                if (category != null)
                {
                    cmbCategory.SelectedItem = category;
                }
            }
        }
        
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;
                
            try
            {
                // Update component properties
                _component.Name = txtName.Text.Trim();
                _component.Description = txtDescription.Text.Trim();
                _component.Cost = decimal.Parse(txtCost.Text);
                _component.IsActive = chkIsActive.IsChecked ?? false;
                _component.LastModified = DateTime.Now;
                
                // Update category
                var selectedCategory = cmbCategory.SelectedItem as Category;
                _component.CategoryId = selectedCategory?.Id;
                
                // Save to database
                UpdateComponent(_component);
                
                ErrorDialog.ShowSuccess("Component updated successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error updating component: {ex.Message}", "Error");
            }
        }
        
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ErrorDialog.ShowWarning("Component name is required.", "Validation Error");
                txtName.Focus();
                return false;
            }
            
            if (!decimal.TryParse(txtCost.Text, out decimal cost) || cost < 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid cost.", "Validation Error");
                txtCost.Focus();
                return false;
            }
            

            
            return true;
        }
        
        private void UpdateComponent(Component component)
        {
            using var connection = _databaseContext.GetConnection();
            var sql = @"
                UPDATE Components 
                SET Name = @Name, Description = @Description, CategoryId = @CategoryId, Cost = @Cost, 
                    IsActive = @IsActive, LastModified = @LastModified 
                WHERE Id = @Id";
                
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", component.Name);
            command.Parameters.AddWithValue("@Description", component.Description ?? "");
            command.Parameters.AddWithValue("@CategoryId", (object)component.CategoryId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Cost", component.Cost);

            command.Parameters.AddWithValue("@IsActive", component.IsActive);
            command.Parameters.AddWithValue("@LastModified", component.LastModified);
            command.Parameters.AddWithValue("@Id", component.Id);
            
            command.ExecuteNonQuery();
        }
    }
} 