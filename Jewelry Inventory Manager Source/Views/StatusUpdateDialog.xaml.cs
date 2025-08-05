using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;

namespace Moonglow_DB.Views
{
    public partial class StatusUpdateDialog : Window
    {
        public TransferStatus SelectedStatus { get; private set; }
        private TransferStatus _currentStatus;

        public StatusUpdateDialog(TransferStatus currentStatus)
        {
            InitializeComponent();
            _currentStatus = currentStatus;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            // Set current status
            txtCurrentStatus.Text = $"Current Status: {_currentStatus}";

            // Populate status options
            var allStatuses = new List<TransferStatus> 
            { 
                TransferStatus.Created, 
                TransferStatus.InTransit, 
                TransferStatus.Delivered, 
                TransferStatus.Completed, 
                TransferStatus.Cancelled 
            };
            
            foreach (var status in allStatuses)
            {
                var item = new ComboBoxItem 
                { 
                    Content = status.ToString(), 
                    Tag = status 
                };
                cmbStatus.Items.Add(item);
                
                // Select the current status
                if (status == _currentStatus)
                {
                    cmbStatus.SelectedItem = item;
                }
            }
            
            // If no item was selected (fallback), select the first one
            if (cmbStatus.SelectedItem == null)
            {
                cmbStatus.SelectedIndex = 0;
            }

            // Set help text
            txtHelp.Text = GetStatusHelpText(_currentStatus);
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStatus.SelectedItem is ComboBoxItem selectedItem)
            {
                SelectedStatus = (TransferStatus)selectedItem.Tag;
                DialogResult = true;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private string GetStatusHelpText(TransferStatus currentStatus)
        {
            return currentStatus switch
            {
                TransferStatus.Created => "📦 Created: Transfer order has been created and items are being prepared for shipment.\n\nYou can skip to any status, including directly to Completed if the transfer has already been processed.",
                TransferStatus.InTransit => "🚚 In Transit: Items have been shipped and are en route to the destination.\n\nYou can skip to Delivered or Completed if the items have already arrived.",
                TransferStatus.Delivered => "📦 Delivered: Items have arrived at the destination location.\n\nReady to execute transfer to update stock levels.",
                TransferStatus.Completed => "✅ Completed: Transfer has been executed and stock levels have been updated.\n\nThis is the final status for successful transfers.",
                TransferStatus.Cancelled => "❌ Cancelled: Transfer has been cancelled and will not be processed.\n\nYou can reactivate by changing to any other status.",
                _ => "Select the new status for this transfer. You can skip intermediate states if needed."
            };
        }
    }
} 