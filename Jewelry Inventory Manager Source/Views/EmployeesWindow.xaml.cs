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
    public partial class EmployeesWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<Employee> _allEmployees;
        private List<Employee> _filteredEmployees;

        public EmployeesWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                _allEmployees = GetAllEmployees();
                _filteredEmployees = new List<Employee>(_allEmployees);
                dgEmployees.ItemsSource = _filteredEmployees;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading employees: {ex.Message}", "Error");
                // Initialize empty lists to prevent null reference exceptions
                _allEmployees = new List<Employee>();
                _filteredEmployees = new List<Employee>();
                dgEmployees.ItemsSource = _filteredEmployees;
            }
        }

        private List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();
            
            using var connection = _databaseContext.GetConnection();
            var sql = @"
                SELECT Id, FirstName, LastName, Email, Phone, CommissionRate, IsActive, HireDate, CreatedDate
                FROM Employees 
                ORDER BY LastName, FirstName";
            
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                employees.Add(new Employee
                {
                    Id = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Phone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    CommissionRate = reader.GetDecimal(5),
                    IsActive = reader.GetBoolean(6),
                    HireDate = reader.GetDateTime(7),
                    CreatedDate = reader.GetDateTime(8)
                });
            }
            
            return employees;
        }

        private void UpdateButtonStates()
        {
            var hasSelection = dgEmployees.SelectedItem != null;
            btnEdit.IsEnabled = hasSelection;
            btnDelete.IsEnabled = hasSelection;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded && _allEmployees != null)
            {
                ApplyFilters();
            }
        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && _allEmployees != null)
            {
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            // Ensure employees are loaded
            if (_allEmployees == null)
            {
                _allEmployees = new List<Employee>();
            }

            var searchText = txtSearch.Text.ToLower();
            var filterText = (cmbFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

            _filteredEmployees = _allEmployees.Where(employee =>
            {
                // Search filter
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                  employee.FirstName.ToLower().Contains(searchText) ||
                                  employee.LastName.ToLower().Contains(searchText) ||
                                  employee.FullName.ToLower().Contains(searchText) ||
                                  employee.Email.ToLower().Contains(searchText) ||
                                  employee.Phone.ToLower().Contains(searchText);

                // Status filter
                var matchesFilter = filterText switch
                {
                    "Active Only" => employee.IsActive,
                    "Inactive Only" => !employee.IsActive,
                    _ => true // "All Employees"
                };

                return matchesSearch && matchesFilter;
            }).ToList();

            dgEmployees.ItemsSource = null;
            dgEmployees.ItemsSource = _filteredEmployees;
        }

        private void dgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addEmployeeWindow = new AddEmployeeWindow();
                if (addEmployeeWindow.ShowDialog() == true)
                {
                    LoadEmployees();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening add employee window: {ex.Message}", "Error");
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = dgEmployees.SelectedItem as Employee;
            if (selectedEmployee == null)
            {
                ErrorDialog.ShowWarning("Please select an employee to edit.", "No Selection");
                return;
            }

            var editEmployeeWindow = new EditEmployeeWindow(_databaseContext, selectedEmployee);
            if (editEmployeeWindow.ShowDialog() == true)
            {
                LoadEmployees();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = dgEmployees.SelectedItem as Employee;
            if (selectedEmployee == null)
            {
                ErrorDialog.ShowWarning("Please select an employee to delete.", "No Selection");
                return;
            }

            var result = ErrorDialog.ShowConfirmation(
                $"Are you sure you want to delete the employee '{selectedEmployee.FullName}'?\n\nThis action cannot be undone.",
                "Confirm Delete");

            if (result)
            {
                try
                {
                    DeleteEmployee(selectedEmployee.Id);
                    LoadEmployees();
                    ErrorDialog.ShowSuccess("Employee deleted successfully!", "Success");
                }
                catch (Exception ex)
                {
                    ErrorDialog.ShowError($"Error deleting employee: {ex.Message}", "Error");
                }
            }
        }

        private void DeleteEmployee(int employeeId)
        {
            using var connection = _databaseContext.GetConnection();
            var sql = "DELETE FROM Employees WHERE Id = @Id";
            
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", employeeId);
            command.ExecuteNonQuery();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees();
        }
    }
} 