using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class AddEmployeeWindow : Window
    {
        public AddEmployeeWindow()
        {
            InitializeComponent();
            dpHireDate.SelectedDate = DateTime.Today;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                SaveEmployee();
                ErrorDialog.ShowSuccess("Employee added successfully!", "Success");
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving employee: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                ErrorDialog.ShowWarning("Please enter a first name.", "Validation Error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                ErrorDialog.ShowWarning("Please enter a last name.", "Validation Error");
                return false;
            }

            if (dpHireDate.SelectedDate == null)
            {
                ErrorDialog.ShowWarning("Please select a hire date.", "Validation Error");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                if (!IsValidEmail(txtEmail.Text))
                {
                    ErrorDialog.ShowWarning("Please enter a valid email address.", "Validation Error");
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(txtCommissionRate.Text))
            {
                if (!decimal.TryParse(txtCommissionRate.Text, out decimal rate) || rate < 0 || rate > 100)
                {
                    ErrorDialog.ShowWarning("Please enter a valid commission rate between 0 and 100.", "Validation Error");
                    return false;
                }
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void SaveEmployee()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                using var dbContext = new DatabaseContext(connectionString);
                
                var sql = @"
                    INSERT INTO Employees (FirstName, LastName, Email, Phone, CommissionRate, HireDate, IsActive, CreatedDate, LastModified) 
                    VALUES (@firstName, @lastName, @email, @phone, @commissionRate, @hireDate, @isActive, @createdDate, @lastModified)";

                using var command = dbContext.CreateCommand(sql);
                command.Parameters.AddWithValue("@firstName", txtFirstName.Text.Trim());
                command.Parameters.AddWithValue("@lastName", txtLastName.Text.Trim());
                command.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                command.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                command.Parameters.AddWithValue("@commissionRate", decimal.Parse(txtCommissionRate.Text));
                command.Parameters.AddWithValue("@hireDate", dpHireDate.SelectedDate ?? DateTime.Today);
                command.Parameters.AddWithValue("@isActive", chkIsActive.IsChecked ?? true);
                command.Parameters.AddWithValue("@createdDate", DateTime.Now);
                command.Parameters.AddWithValue("@lastModified", DateTime.Now);
                
                command.ExecuteNonQuery();
                
                ErrorDialog.ShowSuccess("Employee saved successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving employee: {ex.Message}", "Database Error");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtCommissionRate_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers and decimal point
            var regex = new System.Text.RegularExpressions.Regex(@"^\d*\.?\d*$");
            e.Handled = !regex.IsMatch(e.Text);
        }
    }
} 