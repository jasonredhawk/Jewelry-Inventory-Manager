using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class AddLocationWindow : Window
    {
        private readonly DatabaseContext _databaseContext;

        public AddLocationWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                SaveLocation();
                ErrorDialog.ShowSuccess("Location added successfully!", "Success");
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving location: {ex.Message}", "Error");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ErrorDialog.ShowWarning("Please enter a location name.", "Validation Error");
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

        private void SaveLocation()
        {
            try
            {
                var sql = @"
                    INSERT INTO Locations (Name, Address, Phone, Email, IsOnline, IsActive, Notes, CreatedDate, LastModified) 
                    VALUES (@name, @address, @phone, @email, @isOnline, @isActive, @notes, @createdDate, @lastModified)";

                using var command = _databaseContext.CreateCommand(sql);
                command.Parameters.AddWithValue("@name", txtName.Text.Trim());
                command.Parameters.AddWithValue("@address", txtAddress.Text.Trim());
                command.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                command.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                command.Parameters.AddWithValue("@isOnline", chkIsOnline.IsChecked ?? false);
                command.Parameters.AddWithValue("@isActive", chkIsActive.IsChecked ?? true);
                command.Parameters.AddWithValue("@notes", txtNotes.Text.Trim());
                command.Parameters.AddWithValue("@createdDate", DateTime.Now);
                command.Parameters.AddWithValue("@lastModified", DateTime.Now);
                
                command.ExecuteNonQuery();
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error saving location: {ex.Message}", "Database Error");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 