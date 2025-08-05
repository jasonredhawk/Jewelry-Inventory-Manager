using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Moonglow_DB.Data;
using Moonglow_DB.Views;

namespace Moonglow_DB
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                // Initialize database connection
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);

                if (string.IsNullOrEmpty(connectionString))
                {
                    ErrorDialog.ShowWarning("Failed to connect to MySQL database. Please ensure MySQL is running and the connection string is correct.", "Connection Error");
                    Shutdown();
                    return;
                }

                // Create database context and initialize database
                var dbContext = new DatabaseContext(connectionString);
                dbContext.CreateDatabase();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error initializing database: {ex.Message}", "Initialization Error");
                Shutdown();
            }
        }
    }
}
