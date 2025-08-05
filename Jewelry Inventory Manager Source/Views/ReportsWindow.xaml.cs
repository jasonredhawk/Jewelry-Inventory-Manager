using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class ReportsWindow
    {
        private readonly DatabaseContext _databaseContext;

        public ReportsWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
        }
    }
} 