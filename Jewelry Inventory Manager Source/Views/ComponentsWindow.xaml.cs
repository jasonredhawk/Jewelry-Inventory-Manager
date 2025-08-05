using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class ComponentsWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<Component> _allComponents;
        private Component _selectedComponent;

        public ComponentsWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadComponents();
        }

        private void LoadComponents()
        {
            try
            {
                _allComponents = GetAllComponents();
                dgComponents.ItemsSource = _allComponents;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading components: {ex.Message}", "Error");
            }
        }

        private List<Component> GetAllComponents()
        {
            var components = new List<Component>();
            
            using var connection = _databaseContext.GetConnection();
            var sql = @"
                SELECT c.Id, c.SKU, c.Name, c.Description, c.CategoryId, c.Cost, c.CreatedDate, c.LastModified, c.IsActive,
                       cat.Name as CategoryName
                FROM Components c
                LEFT JOIN Categories cat ON c.CategoryId = cat.Id
                ORDER BY c.Name";
            
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                var component = new Component
                {
                    Id = reader.GetInt32(0),
                    SKU = reader.GetString(1),
                    Name = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    CategoryId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                    Cost = reader.GetDecimal(5),
                    CreatedDate = reader.GetDateTime(6),
                    LastModified = reader.GetDateTime(7),
                    IsActive = reader.GetBoolean(8)
                };

                // Set category if exists
                if (!reader.IsDBNull(9))
                {
                    component.Category = new Category { Name = reader.GetString(9) };
                }

                components.Add(component);
            }
            
            return components;
        }

        private void btnAddComponent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addComponentWindow = new AddComponentWindow();
                if (addComponentWindow.ShowDialog() == true)
                {
                    LoadComponents();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening add component window: {ex.Message}", "Error");
            }
        }

        private void btnEditComponent_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedComponent == null)
            {
                ErrorDialog.ShowInformation("Please select a component to edit.", "No Selection");
                return;
            }
            
            var editWindow = new EditComponentWindow(_databaseContext, _selectedComponent);
            if (editWindow.ShowDialog() == true)
            {
                // Refresh the data grid
                LoadComponents();
            }
        }

        private void btnDeleteComponent_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedComponent == null)
            {
                ErrorDialog.ShowInformation("Please select a component to delete.", "No Selection");
                return;
            }

                        var result = ErrorDialog.ShowConfirmation($"Are you sure you want to delete component '{_selectedComponent.Name}'?",
                "Confirm Delete");
            
            if (result)
            {
                try
                {
                    DeleteComponent(_selectedComponent.Id);
                    LoadComponents();
                    ErrorDialog.ShowSuccess("Component deleted successfully.", "Success");
                }
                catch (Exception ex)
                {
                    ErrorDialog.ShowError($"Error deleting component: {ex.Message}", "Error");
                }
            }
        }

        private void DeleteComponent(int componentId)
        {
            using var connection = _databaseContext.GetConnection();
            var sql = "DELETE FROM Components WHERE Id = @Id";
            
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", componentId);
            command.ExecuteNonQuery();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtSearch.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgComponents.ItemsSource = _allComponents;
            }
            else
            {
                var filteredComponents = _allComponents.Where(c => 
                    c.Name.ToLower().Contains(searchText) ||
                    c.SKU.ToLower().Contains(searchText) ||
                    c.Description.ToLower().Contains(searchText)
                ).ToList();
                
                dgComponents.ItemsSource = filteredComponents;
            }
        }

        private void dgComponents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedComponent = dgComponents.SelectedItem as Component;
            btnEditComponent.IsEnabled = _selectedComponent != null;
            btnDeleteComponent.IsEnabled = _selectedComponent != null;
        }
    }
} 