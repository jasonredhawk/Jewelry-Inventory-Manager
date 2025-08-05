using System;
using System.Windows;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class EditEmployeeWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private readonly Employee _employee;

        public EditEmployeeWindow(DatabaseContext databaseContext, Employee employee)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _employee = employee;
            
            LoadEmployeeData();
        }

        private void LoadEmployeeData()
        {
            txtFirstName.Text = _employee.FirstName;
            txtLastName.Text = _employee.LastName;
            txtEmail.Text = _employee.Email ?? "";
            txtPhone.Text = _employee.Phone ?? "";
            txtCommissionRate.Text = _employee.CommissionRate.ToString("F2");
            dpHireDate.SelectedDate = _employee.HireDate;
            chkIsActive.IsChecked = _employee.IsActive;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                UpdateEmployee();
                ErrorDialog.ShowSuccess("Employee updated successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error updating employee: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                ErrorDialog.ShowWarning("Please enter a first name.", "Validation Error");
                txtFirstName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                ErrorDialog.ShowWarning("Please enter a last name.", "Validation Error");
                txtLastName.Focus();
                return false;
            }

            if (dpHireDate.SelectedDate == null)
            {
                ErrorDialog.ShowWarning("Please select a hire date.", "Validation Error");
                dpHireDate.Focus();
                return false;
            }

            // Validate email format if provided
            if (!string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                try
                {
                    var email = new System.Net.Mail.MailAddress(txtEmail.Text);
                }
                catch
                {
                    ErrorDialog.ShowWarning("Please enter a valid email address.", "Validation Error");
                    txtEmail.Focus();
                    return false;
                }
            }

            // Validate commission rate
            if (!decimal.TryParse(txtCommissionRate.Text, out decimal commissionRate) || commissionRate < 0 || commissionRate > 100)
            {
                ErrorDialog.ShowWarning("Please enter a valid commission rate between 0 and 100.", "Validation Error");
                txtCommissionRate.Focus();
                return false;
            }

            return true;
        }

        private void UpdateEmployee()
        {
            using var connection = _databaseContext.GetConnection();
            var sql = @"
                UPDATE Employees 
                SET FirstName = @FirstName, LastName = @LastName, Email = @Email, 
                    Phone = @Phone, CommissionRate = @CommissionRate, IsActive = @IsActive, 
                    HireDate = @HireDate
                WHERE Id = @Id";
            
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", _employee.Id);
            command.Parameters.AddWithValue("@FirstName", txtFirstName.Text.Trim());
            command.Parameters.AddWithValue("@LastName", txtLastName.Text.Trim());
            command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim());
            command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim());
            command.Parameters.AddWithValue("@CommissionRate", decimal.Parse(txtCommissionRate.Text));
            command.Parameters.AddWithValue("@IsActive", chkIsActive.IsChecked ?? true);
            command.Parameters.AddWithValue("@HireDate", dpHireDate.SelectedDate.Value);
            
            command.ExecuteNonQuery();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 