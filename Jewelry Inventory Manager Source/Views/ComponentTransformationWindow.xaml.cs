using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class ComponentTransformationWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<Component> _allComponents;
        private List<Location> _allLocations;
        private List<TransformationItem> _sourceItems;
        private List<TransformationItem> _resultItems;
        private Component _selectedSourceComponent;

        public ComponentTransformationWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _sourceItems = new List<TransformationItem>();
            _resultItems = new List<TransformationItem>();
            
            // Defer UI updates until window is loaded
            this.Loaded += ComponentTransformationWindow_Loaded;
        }

        private void ComponentTransformationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadComponents();
            LoadLocations();
            UpdateSummary();
        }

        private void LoadComponents()
        {
            try
            {
                _allComponents = _databaseContext.GetAllComponents();
                cmbSourceComponent.ItemsSource = _allComponents;
                cmbAddComponent.ItemsSource = _allComponents;
                cmbResultComponent.ItemsSource = _allComponents;
                cmbCombineResultComponent.ItemsSource = _allComponents;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading components: {ex.Message}", "Error");
            }
        }

        private void LoadLocations()
        {
            try
            {
                _allLocations = _databaseContext.GetAllLocations();
                cmbLocation.ItemsSource = _allLocations;
                if (_allLocations.Count > 0)
                    cmbLocation.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading locations: {ex.Message}", "Error");
            }
        }



        private void rbBreakDown_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) // Only update UI if window is loaded
            {
                UpdateUIForBreakDown();
                UpdateSummary();
            }
        }

        private void rbCombine_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) // Only update UI if window is loaded
            {
                UpdateUIForCombine();
                UpdateSummary();
            }
        }

        private void UpdateUIForBreakDown()
        {
            txtSourceTitle.Text = "Source Component:";
            pnlBreakDown.Visibility = Visibility.Visible;
            pnlCombine.Visibility = Visibility.Collapsed;
            pnlBreakDownResults.Visibility = Visibility.Visible;
            pnlCombineResults.Visibility = Visibility.Collapsed;
            txtResultTitle.Text = "Result Components:";
            
            // Clear data grids
            _sourceItems.Clear();
            _resultItems.Clear();
            if (dgSourceComponents != null)
                dgSourceComponents.ItemsSource = null;
            if (dgResultComponents != null)
                dgResultComponents.ItemsSource = null;
        }

        private void UpdateUIForCombine()
        {
            txtSourceTitle.Text = "Source Components:";
            pnlBreakDown.Visibility = Visibility.Collapsed;
            pnlCombine.Visibility = Visibility.Visible;
            pnlBreakDownResults.Visibility = Visibility.Collapsed;
            pnlCombineResults.Visibility = Visibility.Visible;
            txtResultTitle.Text = "New Component:";
            
            // Clear data grids
            _sourceItems.Clear();
            _resultItems.Clear();
            if (dgSourceComponents != null)
                dgSourceComponents.ItemsSource = null;
            if (dgResultComponents != null)
                dgResultComponents.ItemsSource = null;
        }

        private void cmbSourceComponent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedSourceComponent = cmbSourceComponent.SelectedItem as Component;
            UpdateSummary();
        }

        private void btnAddComponent_Click(object sender, RoutedEventArgs e)
        {
            var selectedComponent = cmbAddComponent.SelectedItem as Component;
            if (selectedComponent == null)
            {
                ErrorDialog.ShowWarning("Please select a component to add.", "No Selection");
                return;
            }

            if (!int.TryParse(txtAddQuantity.Text, out int quantity) || quantity <= 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid quantity.", "Invalid Quantity");
                return;
            }

            // Check if component already exists in source items
            var existingItem = _sourceItems.FirstOrDefault(x => x.ComponentId == selectedComponent.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var newItem = new TransformationItem
                {
                    ComponentId = selectedComponent.Id,
                    ComponentName = selectedComponent.Name,
                    ComponentSKU = selectedComponent.SKU,
                    Quantity = quantity,
                    UnitCost = selectedComponent.Cost
                };
                _sourceItems.Add(newItem);
            }

            if (dgSourceComponents != null)
            {
                dgSourceComponents.ItemsSource = null;
                dgSourceComponents.ItemsSource = _sourceItems;
            }
            UpdateSummary();
        }

        private void btnRemoveComponent_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgSourceComponents?.SelectedItem as TransformationItem;
            if (selectedItem != null)
            {
                _sourceItems.Remove(selectedItem);
                if (dgSourceComponents != null)
                {
                    dgSourceComponents.ItemsSource = null;
                    dgSourceComponents.ItemsSource = _sourceItems;
                }
                UpdateSummary();
            }
        }

        private void btnAddResult_Click(object sender, RoutedEventArgs e)
        {
            var selectedComponent = cmbResultComponent.SelectedItem as Component;
            if (selectedComponent == null)
            {
                ErrorDialog.ShowWarning("Please select a result component.", "No Selection");
                return;
            }

            if (!int.TryParse(txtResultQuantity.Text, out int quantity) || quantity <= 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid quantity.", "Invalid Quantity");
                return;
            }

            var newItem = new TransformationItem
            {
                ComponentId = selectedComponent.Id,
                ComponentName = selectedComponent.Name,
                ComponentSKU = selectedComponent.SKU,
                Quantity = quantity,
                UnitCost = selectedComponent.Cost
            };
            _resultItems.Add(newItem);

            if (dgResultComponents != null)
            {
                dgResultComponents.ItemsSource = null;
                dgResultComponents.ItemsSource = _resultItems;
            }
            UpdateSummary();
        }

        private void btnAddCombineResult_Click(object sender, RoutedEventArgs e)
        {
            var selectedComponent = cmbCombineResultComponent.SelectedItem as Component;
            if (selectedComponent == null)
            {
                ErrorDialog.ShowWarning("Please select a result component.", "No Selection");
                return;
            }

            if (!int.TryParse(txtCombineResultQuantity.Text, out int quantity) || quantity <= 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid quantity.", "Invalid Quantity");
                return;
            }

            var newItem = new TransformationItem
            {
                ComponentId = selectedComponent.Id,
                ComponentName = selectedComponent.Name,
                ComponentSKU = selectedComponent.SKU,
                Quantity = quantity,
                UnitCost = selectedComponent.Cost
            };
            _resultItems.Add(newItem);

            if (dgResultComponents != null)
            {
                dgResultComponents.ItemsSource = null;
                dgResultComponents.ItemsSource = _resultItems;
            }
            UpdateSummary();
        }

        private void btnRemoveResult_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgResultComponents?.SelectedItem as TransformationItem;
            if (selectedItem != null)
            {
                _resultItems.Remove(selectedItem);
                if (dgResultComponents != null)
                {
                    dgResultComponents.ItemsSource = null;
                    dgResultComponents.ItemsSource = _resultItems;
                }
                UpdateSummary();
            }
        }

        private void txtQuantity_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void UpdateSummary()
        {
            var summary = new List<string>();

            if (rbBreakDown?.IsChecked == true)
            {
                // Break Down Mode
                if (_selectedSourceComponent != null)
                {
                    var sourceQuantity = int.TryParse(txtSourceQuantity?.Text, out int qty) ? qty : 0;
                    summary.Add($"🔽 Breaking down: {sourceQuantity}x {_selectedSourceComponent.Name}");
                    summary.Add($"   Source Cost: {sourceQuantity * _selectedSourceComponent.Cost:C}");
                }

                if (_resultItems.Count > 0)
                {
                    summary.Add($"");
                    summary.Add($"📦 Result Components:");
                    foreach (var item in _resultItems)
                    {
                        summary.Add($"   • {item.Quantity}x {item.ComponentName} = {item.TotalCost:C}");
                    }
                }
            }
            else if (rbCombine?.IsChecked == true)
            {
                // Combine Mode
                if (_sourceItems.Count > 0)
                {
                    summary.Add($"🔗 Combining Components:");
                    foreach (var item in _sourceItems)
                    {
                        summary.Add($"   • {item.Quantity}x {item.ComponentName} = {item.TotalCost:C}");
                    }
                }

                if (_resultItems.Count > 0)
                {
                    summary.Add($"");
                    summary.Add($"📦 Result Components:");
                    foreach (var item in _resultItems)
                    {
                        summary.Add($"   • {item.Quantity}x {item.ComponentName} = {item.TotalCost:C}");
                    }
                }
            }

            if (cmbLocation?.SelectedItem != null)
            {
                var location = cmbLocation.SelectedItem as Location;
                summary.Add($"");
                summary.Add($"📍 Location: {location.Name}");
            }

            if (txtSummary != null)
            {
                txtSummary.Text = string.Join("\n", summary);
            }
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateTransformation())
                return;

            try
            {
                if (rbBreakDown.IsChecked == true)
                {
                    ExecuteBreakDown();
                }
                else
                {
                    ExecuteCombine();
                }

                ErrorDialog.ShowSuccess("Transformation executed successfully!", "Success");
                Close();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error executing transformation: {ex.Message}", "Error");
            }
        }

        private bool ValidateTransformation()
        {
            if (cmbLocation.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select a location.", "Validation Error");
                return false;
            }

            if (rbBreakDown.IsChecked == true)
            {
                if (_selectedSourceComponent == null)
                {
                    ErrorDialog.ShowWarning("Please select a source component to break down.", "Validation Error");
                    return false;
                }

                if (!int.TryParse(txtSourceQuantity.Text, out int sourceQty) || sourceQty <= 0)
                {
                    ErrorDialog.ShowWarning("Please enter a valid source quantity.", "Validation Error");
                    return false;
                }

                if (_resultItems.Count == 0)
                {
                    ErrorDialog.ShowWarning("Please add at least one result component.", "Validation Error");
                    return false;
                }
            }
            else
            {
                if (_sourceItems.Count == 0)
                {
                    ErrorDialog.ShowWarning("Please add at least one source component.", "Validation Error");
                    return false;
                }

                if (_resultItems.Count == 0)
                {
                    ErrorDialog.ShowWarning("Please add at least one result component.", "Validation Error");
                    return false;
                }
            }

            return true;
        }

        private void ExecuteBreakDown()
        {
            var location = cmbLocation.SelectedItem as Location;
            var sourceQty = int.Parse(txtSourceQuantity.Text);

            using var connection = _databaseContext.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Reduce source component stock in LocationInventory
                var updateSourceSql = @"
                    UPDATE LocationInventory 
                    SET CurrentStock = CurrentStock - @Quantity, LastModified = @LastModified 
                    WHERE LocationId = @LocationId AND ItemType = 'Component' AND ItemId = @ComponentId";
                
                using var updateSourceCommand = new MySqlCommand(updateSourceSql, connection);
                updateSourceCommand.Transaction = transaction;
                updateSourceCommand.Parameters.AddWithValue("@Quantity", sourceQty);
                updateSourceCommand.Parameters.AddWithValue("@LastModified", DateTime.Now);
                updateSourceCommand.Parameters.AddWithValue("@LocationId", location.Id);
                updateSourceCommand.Parameters.AddWithValue("@ComponentId", _selectedSourceComponent.Id);
                updateSourceCommand.ExecuteNonQuery();

                // Add inventory transaction for source component reduction
                var insertSourceTransactionSql = @"
                    INSERT INTO InventoryTransactions (TransactionType, ItemType, ItemId, LocationId, Quantity, Notes, TransactionDate)
                    VALUES (@TransactionType, @ItemType, @ItemId, @LocationId, @Quantity, @Notes, @TransactionDate)";
                
                using var insertSourceTransactionCommand = new MySqlCommand(insertSourceTransactionSql, connection);
                insertSourceTransactionCommand.Transaction = transaction;
                insertSourceTransactionCommand.Parameters.AddWithValue("@TransactionType", "BreakDown");
                insertSourceTransactionCommand.Parameters.AddWithValue("@ItemType", "Component");
                insertSourceTransactionCommand.Parameters.AddWithValue("@ItemId", _selectedSourceComponent.Id);
                insertSourceTransactionCommand.Parameters.AddWithValue("@LocationId", location.Id);
                insertSourceTransactionCommand.Parameters.AddWithValue("@Quantity", -sourceQty);
                insertSourceTransactionCommand.Parameters.AddWithValue("@Notes", $"Break down transformation: {txtNotes.Text}");
                insertSourceTransactionCommand.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
                insertSourceTransactionCommand.ExecuteNonQuery();

                // Increase result component stocks
                foreach (var resultItem in _resultItems)
                {
                    var updateResultSql = @"
                        UPDATE LocationInventory 
                        SET CurrentStock = CurrentStock + @Quantity, LastModified = @LastModified 
                        WHERE LocationId = @LocationId AND ItemType = 'Component' AND ItemId = @ComponentId";
                    
                    using var updateResultCommand = new MySqlCommand(updateResultSql, connection);
                    updateResultCommand.Transaction = transaction;
                    updateResultCommand.Parameters.AddWithValue("@Quantity", resultItem.Quantity);
                    updateResultCommand.Parameters.AddWithValue("@LastModified", DateTime.Now);
                    updateResultCommand.Parameters.AddWithValue("@LocationId", location.Id);
                    updateResultCommand.Parameters.AddWithValue("@ComponentId", resultItem.ComponentId);
                    updateResultCommand.ExecuteNonQuery();

                    // Add inventory transaction for result component increase
                    var insertResultTransactionSql = @"
                        INSERT INTO InventoryTransactions (TransactionType, ItemType, ItemId, LocationId, Quantity, Notes, TransactionDate)
                        VALUES (@TransactionType, @ItemType, @ItemId, @LocationId, @Quantity, @Notes, @TransactionDate)";
                    
                    using var insertResultTransactionCommand = new MySqlCommand(insertResultTransactionSql, connection);
                    insertResultTransactionCommand.Transaction = transaction;
                    insertResultTransactionCommand.Parameters.AddWithValue("@TransactionType", "Adjustment");
                    insertResultTransactionCommand.Parameters.AddWithValue("@ItemType", "Component");
                    insertResultTransactionCommand.Parameters.AddWithValue("@ItemId", resultItem.ComponentId);
                    insertResultTransactionCommand.Parameters.AddWithValue("@LocationId", location.Id);
                    insertResultTransactionCommand.Parameters.AddWithValue("@Quantity", resultItem.Quantity);
                    insertResultTransactionCommand.Parameters.AddWithValue("@Notes", $"Break down result: {txtNotes.Text}");
                    insertResultTransactionCommand.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
                    insertResultTransactionCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void ExecuteCombine()
        {
            var location = cmbLocation.SelectedItem as Location;

            using var connection = _databaseContext.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Reduce source component stocks
                foreach (var sourceItem in _sourceItems)
                {
                    var updateSourceSql = @"
                        UPDATE LocationInventory 
                        SET CurrentStock = CurrentStock - @Quantity, LastModified = @LastModified 
                        WHERE LocationId = @LocationId AND ItemType = 'Component' AND ItemId = @ComponentId";
                    
                    using var updateSourceCommand = new MySqlCommand(updateSourceSql, connection);
                    updateSourceCommand.Transaction = transaction;
                    updateSourceCommand.Parameters.AddWithValue("@Quantity", sourceItem.Quantity);
                    updateSourceCommand.Parameters.AddWithValue("@LastModified", DateTime.Now);
                    updateSourceCommand.Parameters.AddWithValue("@LocationId", location.Id);
                    updateSourceCommand.Parameters.AddWithValue("@ComponentId", sourceItem.ComponentId);
                    updateSourceCommand.ExecuteNonQuery();

                    // Add inventory transaction for source component reduction
                    var insertSourceTransactionSql = @"
                        INSERT INTO InventoryTransactions (TransactionType, ItemType, ItemId, LocationId, Quantity, Notes, TransactionDate)
                        VALUES (@TransactionType, @ItemType, @ItemId, @LocationId, @Quantity, @Notes, @TransactionDate)";
                    
                    using var insertSourceTransactionCommand = new MySqlCommand(insertSourceTransactionSql, connection);
                    insertSourceTransactionCommand.Transaction = transaction;
                    insertSourceTransactionCommand.Parameters.AddWithValue("@TransactionType", "Adjustment");
                    insertSourceTransactionCommand.Parameters.AddWithValue("@ItemType", "Component");
                    insertSourceTransactionCommand.Parameters.AddWithValue("@ItemId", sourceItem.ComponentId);
                    insertSourceTransactionCommand.Parameters.AddWithValue("@LocationId", location.Id);
                    insertSourceTransactionCommand.Parameters.AddWithValue("@Quantity", -sourceItem.Quantity);
                    insertSourceTransactionCommand.Parameters.AddWithValue("@Notes", $"Combine transformation: {txtNotes.Text}");
                    insertSourceTransactionCommand.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
                    insertSourceTransactionCommand.ExecuteNonQuery();
                }

                // Increase result component stocks
                foreach (var resultItem in _resultItems)
                {
                    var updateResultSql = @"
                        UPDATE LocationInventory 
                        SET CurrentStock = CurrentStock + @Quantity, LastModified = @LastModified 
                        WHERE LocationId = @LocationId AND ItemType = 'Component' AND ItemId = @ComponentId";
                    
                    using var updateResultCommand = new MySqlCommand(updateResultSql, connection);
                    updateResultCommand.Transaction = transaction;
                    updateResultCommand.Parameters.AddWithValue("@Quantity", resultItem.Quantity);
                    updateResultCommand.Parameters.AddWithValue("@LastModified", DateTime.Now);
                    updateResultCommand.Parameters.AddWithValue("@LocationId", location.Id);
                    updateResultCommand.Parameters.AddWithValue("@ComponentId", resultItem.ComponentId);
                    updateResultCommand.ExecuteNonQuery();

                    // Add inventory transaction for result component
                    var insertResultTransactionSql = @"
                        INSERT INTO InventoryTransactions (TransactionType, ItemType, ItemId, LocationId, Quantity, Notes, TransactionDate)
                        VALUES (@TransactionType, @ItemType, @ItemId, @LocationId, @Quantity, @Notes, @TransactionDate)";
                    
                    using var insertResultTransactionCommand = new MySqlCommand(insertResultTransactionSql, connection);
                    insertResultTransactionCommand.Transaction = transaction;
                    insertResultTransactionCommand.Parameters.AddWithValue("@TransactionType", "Adjustment");
                    insertResultTransactionCommand.Parameters.AddWithValue("@ItemType", "Component");
                    insertResultTransactionCommand.Parameters.AddWithValue("@ItemId", resultItem.ComponentId);
                    insertResultTransactionCommand.Parameters.AddWithValue("@LocationId", location.Id);
                    insertResultTransactionCommand.Parameters.AddWithValue("@Quantity", resultItem.Quantity);
                    insertResultTransactionCommand.Parameters.AddWithValue("@Notes", $"Combine result: {txtNotes.Text}");
                    insertResultTransactionCommand.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
                    insertResultTransactionCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 