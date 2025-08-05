# Moonglow Jewelry Management System

A comprehensive WPF .NET Core 5 application for managing jewelry business operations, including inventory tracking, order management, and sales analytics.

## Features

### Core Functionality
- **Products Management**: Track finished jewelry items with SKU, pricing, and stock levels
- **Components Management**: Manage individual jewelry components used to build products
- **Inventory Tracking**: Monitor stock levels across multiple locations
- **Order Management**: Create and track sales orders with customer and employee information
- **Customer Management**: Store customer information including delivery addresses
- **Employee Management**: Track employees for commission and bonus calculations
- **Location Management**: Manage multiple sales locations including online platforms
- **Reports & Analytics**: Generate reports and export data to CSV

### Key Features
- **MySQL Database**: Robust data storage with proper relationships
- **Modern UI**: Clean, intuitive interface with Material Design principles
- **Search Functionality**: Quick search across products and components
- **Stock Alerts**: Automatic low stock notifications
- **Multi-location Support**: Track inventory across physical stores and online
- **Employee Commission Tracking**: Calculate bonuses based on sales performance
- **CSV Export**: Export data for external analysis

## Database Schema

### Core Tables
- **Products**: Finished jewelry items with pricing and stock information
- **Components**: Individual parts used to build products
- **ProductComponents**: Relationship table linking products to their components
- **Locations**: Sales locations (physical stores and online platforms)
- **Employees**: Staff information with commission rates
- **Customers**: Customer data including delivery addresses
- **Orders**: Sales orders with customer, employee, and location tracking
- **OrderItems**: Individual items within orders
- **InventoryTransactions**: All inventory movements and adjustments

## Installation & Setup

### Prerequisites
- .NET Core 5.0 SDK
- MySQL Server (local or remote)
- Visual Studio 2019/2022 or VS Code

### Database Setup
1. Install MySQL Server on your machine or use a remote MySQL server
2. The application will automatically create the database and all necessary tables on first run
3. No manual database setup required - everything is handled automatically

### Application Setup
1. Clone or download the project
2. Open the solution in Visual Studio or VS Code
3. Restore NuGet packages: `dotnet restore`
4. Build the project: `dotnet build`
5. Run the application: `dotnet run`

### First Run
When you first run the application, you'll see a **Database Connection Settings** window where you can:
- Configure your MySQL server connection (Server, Port, Database name, Username, Password)
- Test the connection to ensure it works
- Save the connection settings
- The app will automatically create the `moonglow_jewelry` database and all required tables

### Connection Settings
- **Server**: Your MySQL server address (default: localhost)
- **Port**: MySQL port (default: 3306)
- **Database**: Database name (default: moonglow_jewelry)
- **Username**: MySQL username (default: root)
- **Password**: MySQL password (leave empty if no password)

You can change these settings later by clicking the "⚙ Settings" button in the main window header.

## Usage

### Getting Started
1. Launch the application
2. The system will automatically create the database tables on first run
3. Use the main navigation to access different modules

### Products Management
- Add new jewelry products with SKU, name, description, and pricing
- Set minimum stock levels for automatic alerts
- Track current inventory levels
- Search and filter products

### Components Management
- Manage individual jewelry components
- Track component costs and stock levels
- Link components to products for automatic SKU generation

### Inventory Tracking
- Monitor stock levels across all locations
- Receive alerts for low stock items
- Track inventory movements and adjustments
- Generate purchase orders for restocking

## Architecture

### Technology Stack
- **Frontend**: WPF (.NET Core 5)
- **Database**: MySQL
- **Data Access**: ADO.NET with MySql.Data
- **UI Framework**: XAML with Material Design principles

### Project Structure
```
Moonglow DB/
├── Models/                 # Data models with INotifyPropertyChanged
├── Data/                  # Database context and data access
├── Views/                 # WPF windows and user interfaces
├── MainWindow.xaml        # Main application window
└── README.md             # This documentation
```

## Database Connection

The application uses a dynamic MySQL connection system:

### Connection Management
- **First Run**: The app prompts for database connection settings
- **Settings Button**: Click "⚙ Settings" in the header to change connection settings
- **Connection Testing**: Built-in connection testing before saving settings
- **Automatic Setup**: Creates database and tables automatically on first connection

### Supported Configurations
- **Local MySQL**: `Server=localhost;Port=3306;Database=moonglow_jewelry;Uid=root;Pwd=;`
- **Remote MySQL**: Configure server address, port, and credentials as needed
- **Custom Database**: Change database name in connection settings

## Future Enhancements

### Planned Features
- **Shopify Integration**: Sync orders and inventory with Shopify
- **Advanced Reporting**: Charts and analytics dashboard
- **Barcode Scanning**: QR code generation and scanning
- **Email Notifications**: Automated low stock alerts
- **Backup System**: Automated database backups
- **Multi-user Support**: User authentication and roles

### API Integrations
- Shopify REST API for order synchronization
- Payment gateway integrations
- Shipping provider APIs

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support and questions, please contact the development team or create an issue in the repository.

---

**Note**: This is a comprehensive jewelry management system designed to replace expensive third-party solutions like InFlow while providing the specific functionality needed for jewelry businesses. 