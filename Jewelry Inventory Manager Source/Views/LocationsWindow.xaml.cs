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
    public partial class LocationsWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<Location> _allLocations;
        private List<Location> _filteredLocations;

        public LocationsWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadLocations();
        }

        private void LoadLocations()
        {
            try
            {
                _allLocations = GetAllLocations();
                _filteredLocations = new List<Location>(_allLocations);
                dgLocations.ItemsSource = _filteredLocations;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading locations: {ex.Message}", "Error");
                // Initialize empty lists to prevent null reference exceptions
                _allLocations = new List<Location>();
                _filteredLocations = new List<Location>();
                dgLocations.ItemsSource = _filteredLocations;
            }
        }

        private List<Location> GetAllLocations()
        {
            return _databaseContext.GetAllLocations();
        }

        private void UpdateButtonStates()
        {
            var hasSelection = dgLocations.SelectedItem != null;
            btnEdit.IsEnabled = hasSelection;
            btnDelete.IsEnabled = hasSelection;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded && _allLocations != null)
            {
                ApplyFilters();
            }
        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && _allLocations != null)
            {
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            // Ensure locations are loaded
            if (_allLocations == null)
            {
                _allLocations = new List<Location>();
            }

            var searchText = txtSearch.Text.ToLower();
            var filterText = (cmbFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

            _filteredLocations = _allLocations.Where(location =>
            {
                // Search filter
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                  location.Name.ToLower().Contains(searchText) ||
                                  location.Address.ToLower().Contains(searchText) ||
                                  location.Phone.ToLower().Contains(searchText) ||
                                  location.Email.ToLower().Contains(searchText);

                // Status filter
                var matchesFilter = filterText switch
                {
                    "Active Only" => location.IsActive,
                    "Online Only" => location.IsOnline,
                    _ => true // "All Locations"
                };

                return matchesSearch && matchesFilter;
            }).ToList();

            dgLocations.ItemsSource = null;
            dgLocations.ItemsSource = _filteredLocations;
        }

        private void dgLocations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void btnAddLocation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addLocationWindow = new AddLocationWindow(_databaseContext);
                if (addLocationWindow.ShowDialog() == true)
                {
                    LoadLocations();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening add location window: {ex.Message}", "Error");
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedLocation = dgLocations.SelectedItem as Location;
            if (selectedLocation == null)
            {
                ErrorDialog.ShowWarning("Please select a location to edit.", "No Selection");
                return;
            }

            var editLocationWindow = new EditLocationWindow(_databaseContext, selectedLocation);
            if (editLocationWindow.ShowDialog() == true)
            {
                LoadLocations();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedLocation = dgLocations.SelectedItem as Location;
            if (selectedLocation == null)
            {
                ErrorDialog.ShowWarning("Please select a location to delete.", "No Selection");
                return;
            }

            var result = ErrorDialog.ShowConfirmation(
                $"Are you sure you want to delete the location '{selectedLocation.Name}'?\n\nThis action cannot be undone.",
                "Confirm Delete");

            if (result)
            {
                try
                {
                    DeleteLocation(selectedLocation.Id);
                    LoadLocations();
                    ErrorDialog.ShowSuccess("Location deleted successfully!", "Success");
                }
                catch (Exception ex)
                {
                    ErrorDialog.ShowError($"Error deleting location: {ex.Message}", "Error");
                }
            }
        }

        private void DeleteLocation(int locationId)
        {
            using var connection = _databaseContext.GetConnection();
            var sql = "DELETE FROM Locations WHERE Id = @Id";
            
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", locationId);
            command.ExecuteNonQuery();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLocations();
        }
    }
} 