using System;
using System.Windows;
using Moonglow_DB.Views;
using Moonglow_DB.Data;

namespace Moonglow_DB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var productsWindow = new ProductsWindow(dbContext);
                productsWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening products window: {ex.Message}", "Error");
            }
        }

        private void btnComponents_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var componentsWindow = new ComponentsWindow(dbContext);
                componentsWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening components window: {ex.Message}", "Error");
            }
        }

        private void btnTransformation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var transformationWindow = new ComponentTransformationWindow(dbContext);
                transformationWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening transformation window: {ex.Message}", "Error");
            }
        }

        private void btnCategories_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var categoriesWindow = new CategoriesWindow(dbContext);
                categoriesWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening categories window: {ex.Message}", "Error");
            }
        }

        private void btnInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var inventoryWindow = new InventoryWindow(dbContext);
                inventoryWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error in MainWindow.btnInventory_Click(): {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error");
            }
        }

        private void btnOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var ordersWindow = new OrdersWindow(dbContext);
                ordersWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening orders window: {ex.Message}", "Error");
            }
        }

        private void btnCustomers_Click(object sender, RoutedEventArgs e)
        {
            ErrorDialog.ShowInformation("Customers functionality is temporarily disabled while we fix the application.", "Feature Disabled");
        }

        private void btnEmployees_Click(object sender, RoutedEventArgs e)
        {
            ErrorDialog.ShowInformation("Employees functionality is temporarily disabled while we fix the application.", "Feature Disabled");
        }

        private void btnLocations_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                var connectionString = SettingsManager.BuildConnectionString(settings);
                var dbContext = new DatabaseContext(connectionString);
                
                var locationsWindow = new LocationsWindow(dbContext);
                locationsWindow.ShowDialog();
                
                // Dispose the database context after the window is closed
                dbContext.Dispose();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening locations window: {ex.Message}", "Error");
            }
        }

        private void btnReports_Click(object sender, RoutedEventArgs e)
        {
            ErrorDialog.ShowInformation("Reports functionality is temporarily disabled while we fix the application.", "Feature Disabled");
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var connectionWindow = new Moonglow_DB.Views.ConnectionWindow();
                connectionWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening connection window: {ex.Message}", "Error");
            }
        }
    }
}
