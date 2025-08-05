using System;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class ConnectionWindow : Window
    {
        public string ConnectionString { get; private set; }
        
        public ConnectionWindow()
        {
            InitializeComponent();
            LoadSavedSettings();
        }
        
        private void LoadSavedSettings()
        {
            // Load saved settings or use defaults
            var settings = SettingsManager.LoadSettings();
            txtServer.Text = settings.Server;
            txtPort.Text = settings.Port;
            txtDatabase.Text = settings.Database;
            txtUsername.Text = settings.Username;
            txtPassword.Password = settings.Password;
        }
        

        
        private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;
                
            string connectionString = BuildConnectionString();
            
            try
            {
                using (var dbContext = new DatabaseContext(connectionString))
                {
                    dbContext.TestConnection();
                    ShowStatus("✅ Connection successful! Database is accessible.", true);
                    
                    // Save settings on successful test
                    var settings = new SettingsManager.ConnectionSettings
                    {
                        Server = txtServer.Text,
                        Port = txtPort.Text,
                        Database = txtDatabase.Text,
                        Username = txtUsername.Text,
                        Password = txtPassword.Password
                    };
                    SettingsManager.SaveSettings(settings);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Connection error: {ex.Message}", false);
            }
        }
        
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }
            
            ConnectionString = BuildConnectionString();
            
            try
            {
                using (var dbContext = new DatabaseContext(ConnectionString))
                {
                    dbContext.TestConnection();
                    // Create database and tables if they don't exist
                    dbContext.CreateDatabase();
                    
                    // Save settings on successful connection
                    var settings = new SettingsManager.ConnectionSettings
                    {
                        Server = txtServer.Text,
                        Port = txtPort.Text,
                        Database = txtDatabase.Text,
                        Username = txtUsername.Text,
                        Password = txtPassword.Password
                    };
                    SettingsManager.SaveSettings(settings);
                    
                    ShowStatus("✅ Connection saved and database initialized successfully!", true);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Error saving connection: {ex.Message}", false);
            }
        }
        
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }


        
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtServer.Text))
            {
                ShowStatus("❌ Server address is required.", false);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(txtPort.Text) || !int.TryParse(txtPort.Text, out int port) || port <= 0)
            {
                ShowStatus("❌ Please enter a valid port number.", false);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(txtDatabase.Text))
            {
                ShowStatus("❌ Database name is required.", false);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                ShowStatus("❌ Username is required.", false);
                return false;
            }
            
            return true;
        }
        
        private string BuildConnectionString(bool includeDatabase = true)
        {
            string password = string.IsNullOrEmpty(txtPassword.Password) ? "" : txtPassword.Password;
            
            if (includeDatabase)
            {
                return $"Server={txtServer.Text};Port={txtPort.Text};Database={txtDatabase.Text};Uid={txtUsername.Text};Pwd={password};CharSet=utf8;AllowUserVariables=True;";
            }
            else
            {
                // Connect without specifying a database (to create it)
                return $"Server={txtServer.Text};Port={txtPort.Text};Uid={txtUsername.Text};Pwd={password};CharSet=utf8;AllowUserVariables=True;";
            }
        }
        
        private void ShowStatus(string message, bool isSuccess)
        {
            txtStatus.Text = message;
            
            if (isSuccess)
            {
                borderStatus.Background = System.Windows.Media.Brushes.LightGreen;
                borderStatus.BorderBrush = System.Windows.Media.Brushes.Green;
            }
            else
            {
                borderStatus.Background = System.Windows.Media.Brushes.LightCoral;
                borderStatus.BorderBrush = System.Windows.Media.Brushes.Red;
            }
        }
    }
} 