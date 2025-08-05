using System.Windows;
using Moonglow_DB.Data;
using Moonglow_DB.Models;

namespace Moonglow_DB.Views
{
    public partial class EditOrderWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private readonly Order _order;

        public EditOrderWindow(DatabaseContext databaseContext, Order order)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _order = order;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 