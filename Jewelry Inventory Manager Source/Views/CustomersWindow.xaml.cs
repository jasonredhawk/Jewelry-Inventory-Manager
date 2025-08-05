using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class CustomersWindow
    {
        private readonly DatabaseContext _databaseContext;

        public CustomersWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
        }
    }
} 