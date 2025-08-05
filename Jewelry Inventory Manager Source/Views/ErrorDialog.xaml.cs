using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Moonglow_DB.Views
{
    public partial class ErrorDialog : Window
    {
        public enum DialogType
        {
            Error,
            Warning,
            Information,
            Success,
            Confirmation,
            Disabled
        }

        private DialogType _currentType;

        public ErrorDialog(string message, string title = "Message", DialogType type = DialogType.Information)
        {
            InitializeComponent();
            _currentType = type;
            
            // Set the message
            txtErrorDetails.Text = message;
            
            // Update UI based on dialog type
            UpdateDialogAppearance(type, title);
        }

        private void UpdateDialogAppearance(DialogType type, string customTitle)
        {
            switch (type)
            {
                case DialogType.Error:
                    Title = "Error";
                    txtTitle.Text = "An Error Occurred";
                    txtTitle.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Red
                    txtSubtitle.Text = "You can copy the error details below to share with support.";
                    txtDetailsLabel.Text = "Error Details:";
                    btnClose.Content = "Close";
                    btnCopy.Visibility = Visibility.Visible;
                    txtErrorDetails.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Light red
                    break;

                case DialogType.Warning:
                    Title = "Warning";
                    txtTitle.Text = "Warning";
                    txtTitle.Foreground = new SolidColorBrush(Color.FromRgb(237, 108, 2)); // Orange
                    txtSubtitle.Text = "Please review the details below.";
                    txtDetailsLabel.Text = "Warning Details:";
                    btnClose.Content = "OK";
                    btnCopy.Visibility = Visibility.Visible;
                    txtErrorDetails.Background = new SolidColorBrush(Color.FromRgb(255, 248, 225)); // Light yellow
                    break;

                case DialogType.Success:
                    Title = "Success";
                    txtTitle.Text = "Success!";
                    txtTitle.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Green
                    txtSubtitle.Text = "The operation completed successfully.";
                    txtDetailsLabel.Text = "Details:";
                    btnClose.Content = "OK";
                    btnCopy.Visibility = Visibility.Collapsed;
                    txtErrorDetails.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Light green
                    break;

                case DialogType.Information:
                    Title = "Information";
                    txtTitle.Text = "Information";
                    txtTitle.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)); // Blue
                    txtSubtitle.Text = "Please review the information below.";
                    txtDetailsLabel.Text = "Information:";
                    btnClose.Content = "OK";
                    btnCopy.Visibility = Visibility.Collapsed;
                    txtErrorDetails.Background = new SolidColorBrush(Color.FromRgb(232, 240, 254)); // Light blue
                    break;

                case DialogType.Confirmation:
                    Title = "Confirm";
                    txtTitle.Text = "Confirm Action";
                    txtTitle.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)); // Blue
                    txtSubtitle.Text = "Please confirm your action.";
                    txtDetailsLabel.Text = "Confirmation Details:";
                    btnClose.Content = "Cancel";
                    btnCopy.Visibility = Visibility.Collapsed;
                    txtErrorDetails.Background = new SolidColorBrush(Color.FromRgb(232, 240, 254)); // Light blue
                    break;

                case DialogType.Disabled:
                    Title = "Feature Disabled";
                    txtTitle.Text = "Feature Temporarily Disabled";
                    txtTitle.Foreground = new SolidColorBrush(Color.FromRgb(156, 39, 176)); // Purple
                    txtSubtitle.Text = "This feature is temporarily disabled while we fix the application.";
                    txtDetailsLabel.Text = "Disabled Feature Details:";
                    btnClose.Content = "OK";
                    btnCopy.Visibility = Visibility.Visible;
                    txtErrorDetails.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)); // Light purple
                    break;
            }

            // Override with custom title if provided
            if (!string.IsNullOrEmpty(customTitle) && customTitle != "Message")
            {
                Title = customTitle;
                txtTitle.Text = customTitle;
            }
        }

        public static void ShowError(string errorMessage, string title = "Error")
        {
            var errorDialog = new ErrorDialog(errorMessage, title, DialogType.Error);
            errorDialog.ShowDialog();
        }

        public static void ShowWarning(string message, string title = "Warning")
        {
            var dialog = new ErrorDialog(message, title, DialogType.Warning);
            dialog.ShowDialog();
        }

        public static void ShowInformation(string message, string title = "Information")
        {
            var dialog = new ErrorDialog(message, title, DialogType.Information);
            dialog.ShowDialog();
        }

        public static void ShowSuccess(string message, string title = "Success")
        {
            var dialog = new ErrorDialog(message, title, DialogType.Success);
            dialog.ShowDialog();
        }

        public static bool ShowConfirmation(string message, string title = "Confirm")
        {
            var dialog = new ErrorDialog(message, title, DialogType.Confirmation);
            dialog.SetupConfirmationDialog();
            return dialog.ShowDialog() == true;
        }

        public static void ShowDisabled(string message, string title = "Feature Disabled")
        {
            var dialog = new ErrorDialog(message, title, DialogType.Disabled);
            dialog.ShowDialog();
        }

        private void SetupConfirmationDialog()
        {
            btnCopy.Visibility = Visibility.Collapsed;
            btnYes.Visibility = Visibility.Visible;
            btnNo.Visibility = Visibility.Visible;
            btnClose.Visibility = Visibility.Visible;
            
            // Update button text for confirmation
            btnYes.Content = "Yes";
            btnNo.Content = "No";
            btnClose.Content = "Cancel";
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(txtErrorDetails.Text);
                btnCopy.Content = "Copied!";
                
                // Reset button text after 2 seconds
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, args) =>
                {
                    btnCopy.Content = "Copy to Clipboard";
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                ShowWarning($"Failed to copy to clipboard: {ex.Message}", "Copy Error");
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_currentType == DialogType.Confirmation)
            {
                DialogResult = false;
            }
            Close();
        }
    }
} 