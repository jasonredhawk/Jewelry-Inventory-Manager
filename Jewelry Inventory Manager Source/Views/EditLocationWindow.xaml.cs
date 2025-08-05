using System;
using System.Windows;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class EditLocationWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private readonly Location _location;

        public EditLocationWindow(DatabaseContext databaseContext, Location location)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _location = location;
            LoadLocationData();
        }

        private void LoadLocationData()
        {
            txtName.Text = _location.Name;
            txtAddress.Text = _location.Address ?? "";
            txtPhone.Text = _location.Phone ?? "";
            txtEmail.Text = _location.Email ?? "";
            chkIsOnline.IsChecked = _location.IsOnline;
            chkIsActive.IsChecked = _location.IsActive;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                UpdateLocation();
                ErrorDialog.ShowSuccess("Location updated successfully!", "Success");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error updating location: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ErrorDialog.ShowWarning("Please enter a location name.", "Validation Error");
                txtName.Focus();
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

            return true;
        }

        private void UpdateLocation()
        {
            using var connection = _databaseContext.GetConnection();
            var sql = @"
                UPDATE Locations 
                SET Name = @Name, Address = @Address, Phone = @Phone, Email = @Email, 
                    IsOnline = @IsOnline, IsActive = @IsActive
                WHERE Id = @Id";
            
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", _location.Id);
            command.Parameters.AddWithValue("@Name", txtName.Text.Trim());
            command.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(txtAddress.Text) ? null : txtAddress.Text.Trim());
            command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim());
            command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim());
            command.Parameters.AddWithValue("@IsOnline", chkIsOnline.IsChecked ?? false);
            command.Parameters.AddWithValue("@IsActive", chkIsActive.IsChecked ?? true);
            
            command.ExecuteNonQuery();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 