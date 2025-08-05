using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class BulkTransferManagementWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<BulkTransferOrder> _allTransfers;
        private List<Location> _allLocations;
        private BulkTransferOrder _selectedTransfer;

        public BulkTransferManagementWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _allTransfers = _databaseContext.GetAllBulkTransferOrders();
                _allLocations = _databaseContext.GetAllLocations().Where(l => l.IsActive).ToList();
                
                LoadLocationFilters();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void LoadLocationFilters()
        {
            // Load status filter
            cmbStatusFilter.Items.Clear();
            cmbStatusFilter.Items.Add(new ComboBoxItem { Content = "All Statuses", Tag = "" });
            cmbStatusFilter.Items.Add(new ComboBoxItem { Content = "Created", Tag = "Created" });
            cmbStatusFilter.Items.Add(new ComboBoxItem { Content = "In Transit", Tag = "InTransit" });
            cmbStatusFilter.Items.Add(new ComboBoxItem { Content = "Delivered", Tag = "Delivered" });
            cmbStatusFilter.Items.Add(new ComboBoxItem { Content = "Completed", Tag = "Completed" });
            cmbStatusFilter.Items.Add(new ComboBoxItem { Content = "Cancelled", Tag = "Cancelled" });
            cmbStatusFilter.SelectedIndex = 0;

            // Load location filters
            cmbFromLocationFilter.Items.Clear();
            cmbToLocationFilter.Items.Clear();

            cmbFromLocationFilter.Items.Add(new ComboBoxItem { Content = "All Locations", Tag = (int?)null });
            cmbToLocationFilter.Items.Add(new ComboBoxItem { Content = "All Locations", Tag = (int?)null });

            foreach (var location in _allLocations)
            {
                cmbFromLocationFilter.Items.Add(new ComboBoxItem { Content = location.Name, Tag = location.Id });
                cmbToLocationFilter.Items.Add(new ComboBoxItem { Content = location.Name, Tag = location.Id });
            }

            cmbFromLocationFilter.SelectedIndex = 0;
            cmbToLocationFilter.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            try
            {
                var filteredTransfers = _allTransfers.AsEnumerable();

                // Status filter
                if (cmbStatusFilter.SelectedItem is ComboBoxItem statusItem && statusItem.Tag != null)
                {
                    var statusFilter = statusItem.Tag.ToString();
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        if (Enum.TryParse<TransferStatus>(statusFilter, out var status))
                        {
                            filteredTransfers = filteredTransfers.Where(t => t.Status == status);
                        }
                    }
                }

                // From location filter
                if (cmbFromLocationFilter.SelectedItem is ComboBoxItem fromLocationItem && fromLocationItem.Tag != null)
                {
                    var fromLocationId = (int?)fromLocationItem.Tag;
                    if (fromLocationId.HasValue)
                    {
                        filteredTransfers = filteredTransfers.Where(t => t.FromLocationId == fromLocationId.Value);
                    }
                }

                // To location filter
                if (cmbToLocationFilter.SelectedItem is ComboBoxItem toLocationItem && toLocationItem.Tag != null)
                {
                    var toLocationId = (int?)toLocationItem.Tag;
                    if (toLocationId.HasValue)
                    {
                        filteredTransfers = filteredTransfers.Where(t => t.ToLocationId == toLocationId.Value);
                    }
                }

                // Date range filter
                if (dpFromDate.SelectedDate.HasValue)
                {
                    filteredTransfers = filteredTransfers.Where(t => t.CreatedDate.Date >= dpFromDate.SelectedDate.Value.Date);
                }

                if (dpToDate.SelectedDate.HasValue)
                {
                    filteredTransfers = filteredTransfers.Where(t => t.CreatedDate.Date <= dpToDate.SelectedDate.Value.Date);
                }

                dgTransfers.ItemsSource = filteredTransfers.ToList();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error applying filters: {ex.Message}", "Error");
            }
        }

        private void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbFromLocationFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbToLocationFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void btnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            cmbStatusFilter.SelectedIndex = 0;
            cmbFromLocationFilter.SelectedIndex = 0;
            cmbToLocationFilter.SelectedIndex = 0;
            dpFromDate.SelectedDate = null;
            dpToDate.SelectedDate = null;
            ApplyFilters();
        }

        private void dgTransfers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedTransfer = dgTransfers.SelectedItem as BulkTransferOrder;
            
            btnViewDetails.IsEnabled = _selectedTransfer != null;
            btnUpdateStatus.IsEnabled = _selectedTransfer != null;
            btnExecuteTransfer.IsEnabled = _selectedTransfer != null && 
                                         _selectedTransfer.Status == TransferStatus.Delivered;
        }

        private void btnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTransfer == null) return;

            try
            {
                var transferItems = _databaseContext.GetBulkTransferItems(_selectedTransfer.Id);
                var detailsMessage = $"Transfer Details: {_selectedTransfer.TransferNumber}\n\n";
                detailsMessage += $"From: {_selectedTransfer.FromLocationName}\n";
                detailsMessage += $"To: {_selectedTransfer.ToLocationName}\n";
                detailsMessage += $"Status: {_selectedTransfer.Status}\n";
                detailsMessage += $"Created: {_selectedTransfer.CreatedDate:MM/dd/yyyy HH:mm}\n";
                detailsMessage += $"Notes: {_selectedTransfer.Notes}\n\n";
                detailsMessage += "Items:\n";

                foreach (var item in transferItems)
                {
                    detailsMessage += $"• {item.ItemName} ({item.ItemType}) - Qty: {item.Quantity}\n";
                }

                ErrorDialog.ShowSuccess(detailsMessage, "Transfer Details");
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading transfer details: {ex.Message}", "Error");
            }
        }

        private void btnUpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTransfer == null) return;

            var dialog = new StatusUpdateDialog(_selectedTransfer.Status);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _databaseContext.UpdateBulkTransferStatus(_selectedTransfer.Id, dialog.SelectedStatus);
                    
                    // Refresh the data
                    LoadData();
                    
                    ErrorDialog.ShowSuccess($"Transfer status updated to {dialog.SelectedStatus}", "Status Updated");
                }
                catch (Exception ex)
                {
                    ErrorDialog.ShowError($"Error updating status: {ex.Message}", "Error");
                }
            }
        }

        private void btnExecuteTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTransfer == null || _selectedTransfer.Status != TransferStatus.Delivered) return;

            var result = ErrorDialog.ShowConfirmation(
                $"Are you sure you want to execute this transfer?\n\n" +
                $"This will update the stock levels at the destination location and mark the transfer as completed.\n\n" +
                $"Transfer: {_selectedTransfer.TransferNumber}\n" +
                $"From: {_selectedTransfer.FromLocationName}\n" +
                $"To: {_selectedTransfer.ToLocationName}",
                "Execute Transfer");

            if (result == true)
            {
                try
                {
                    _databaseContext.ExecuteBulkTransfer(_selectedTransfer.Id);
                    
                    // Refresh the data
                    LoadData();
                    
                    ErrorDialog.ShowSuccess(
                        $"Transfer executed successfully!\n\n" +
                        $"Transfer: {_selectedTransfer.TransferNumber}\n" +
                        $"Stock levels have been updated at {_selectedTransfer.ToLocationName}",
                        "Transfer Executed");
                }
                catch (Exception ex)
                {
                    ErrorDialog.ShowError($"Error executing transfer: {ex.Message}", "Error");
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Helper dialog for status updates
    public class StatusUpdateDialog : Window
    {
        public TransferStatus SelectedStatus { get; private set; }

        public StatusUpdateDialog(TransferStatus currentStatus)
        {
            Title = "Update Transfer Status";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lblCurrent = new TextBlock
            {
                Text = $"Current Status: {currentStatus}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10)
            };
            Grid.SetRow(lblCurrent, 0);

            var lblNew = new TextBlock
            {
                Text = "New Status:",
                Margin = new Thickness(10, 20, 10, 5)
            };
            Grid.SetRow(lblNew, 1);

            var cmbStatus = new ComboBox
            {
                Margin = new Thickness(10, 5, 10, 10)
            };
            Grid.SetRow(cmbStatus, 2);

            // Add status options based on current status
            var statuses = GetAvailableStatuses(currentStatus);
            foreach (var status in statuses)
            {
                cmbStatus.Items.Add(new ComboBoxItem { Content = status.ToString(), Tag = status });
            }
            cmbStatus.SelectedIndex = 0;

            var lblHelp = new TextBlock
            {
                Text = GetStatusHelpText(currentStatus),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10, 10, 10, 10),
                FontSize = 11
            };
            Grid.SetRow(lblHelp, 3);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 10, 10, 10)
            };
            Grid.SetRow(buttonPanel, 4);

            var btnOK = new Button
            {
                Content = "Update",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnOK.Click += (s, e) =>
            {
                if (cmbStatus.SelectedItem is ComboBoxItem selectedItem)
                {
                    SelectedStatus = (TransferStatus)selectedItem.Tag;
                    DialogResult = true;
                }
            };

            var btnCancel = new Button
            {
                Content = "Cancel",
                Padding = new Thickness(15, 5, 15, 5)
            };
            btnCancel.Click += (s, e) => DialogResult = false;

            buttonPanel.Children.Add(btnOK);
            buttonPanel.Children.Add(btnCancel);

            grid.Children.Add(lblCurrent);
            grid.Children.Add(lblNew);
            grid.Children.Add(cmbStatus);
            grid.Children.Add(lblHelp);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private List<TransferStatus> GetAvailableStatuses(TransferStatus currentStatus)
        {
            return currentStatus switch
            {
                TransferStatus.Created => new List<TransferStatus> { TransferStatus.InTransit, TransferStatus.Cancelled },
                TransferStatus.InTransit => new List<TransferStatus> { TransferStatus.Delivered, TransferStatus.Cancelled },
                TransferStatus.Delivered => new List<TransferStatus> { TransferStatus.Completed },
                TransferStatus.Completed => new List<TransferStatus> { TransferStatus.Completed },
                TransferStatus.Cancelled => new List<TransferStatus> { TransferStatus.Cancelled },
                _ => new List<TransferStatus> { TransferStatus.Created }
            };
        }

        private string GetStatusHelpText(TransferStatus currentStatus)
        {
            return currentStatus switch
            {
                TransferStatus.Created => "Created: Transfer order has been created and items are being prepared for shipment.",
                TransferStatus.InTransit => "In Transit: Items have been shipped and are en route to the destination.",
                TransferStatus.Delivered => "Delivered: Items have arrived at the destination location. Ready to execute transfer.",
                TransferStatus.Completed => "Completed: Transfer has been executed and stock levels have been updated.",
                TransferStatus.Cancelled => "Cancelled: Transfer has been cancelled and will not be processed.",
                _ => "Select the new status for this transfer."
            };
        }
    }
} 