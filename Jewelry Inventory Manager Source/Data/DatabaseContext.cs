using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using Moonglow_DB.Models;

namespace Moonglow_DB.Data
{
    public class DatabaseContext : IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed = false;

        public DatabaseContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public MySqlConnection GetConnection()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatabaseContext));
            }
            
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public MySqlCommand CreateCommand(string sql)
        {
            var command = new MySqlCommand(sql, GetConnection());
            return command;
        }

        public void Dispose()
        {
            _disposed = true;
        }

        public bool TestConnection()
        {
            try
            {
                // First try to connect without specifying database
                var connectionStringWithoutDb = _connectionString.Replace($"Database={ExtractDatabaseName(_connectionString)};", "");
                using var connection = new MySqlConnection(connectionStringWithoutDb);
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                System.Diagnostics.Debug.WriteLine($"MySQL Connection Error: {ex.Message}");
                throw; // Re-throw the exception so we can see the actual error
            }
        }

        public void CreateDatabase()
        {
            // First, create the database if it doesn't exist
            CreateDatabaseIfNotExists();
            
            // Then create all tables
            using var connection = GetConnection();
            
            // Create tables in order (dependencies first)
            CreateCategoriesTable(connection);
            CreateLocationsTable(connection);
            CreateComponentsTable(connection);
            CreateProductsTable(connection);
            CreateProductComponentsTable(connection);
            CreateLocationInventoryTable(connection);
            CreateInventoryTransactionsTable(connection);
            CreateComponentTransformationsTable(connection);
            CreateTransformationItemsTable(connection);
            CreateEmployeesTable(connection);
            CreateCustomersTable(connection);
            CreateOrdersTable(connection);
            CreateOrderItemsTable(connection);
            CreatePurchaseOrdersTable(connection);
            
            // Update existing database schema if needed
            UpdateDatabaseSchema(connection);
            
            // Only insert sample data if the database is empty (new database)
            if (IsDatabaseEmpty(connection))
            {
                InsertSampleData(connection);
            }
        }
        
        private void CreateDatabaseIfNotExists()
        {
            // Extract database name from connection string
            var databaseName = ExtractDatabaseName(_connectionString);
            if (string.IsNullOrEmpty(databaseName))
                return;
                
            // Create connection string without database
            var connectionStringWithoutDb = _connectionString.Replace($"Database={databaseName};", "");
            
            using var connection = new MySqlConnection(connectionStringWithoutDb);
            connection.Open();
            
            var sql = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`";
            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
        
        private string ExtractDatabaseName(string connectionString)
        {
            // Simple extraction - look for Database= in the connection string
            var databaseIndex = connectionString.IndexOf("Database=");
            if (databaseIndex == -1) return null;
            
            var startIndex = databaseIndex + 9; // "Database=".Length
            var endIndex = connectionString.IndexOf(";", startIndex);
            if (endIndex == -1) endIndex = connectionString.Length;
            
            return connectionString.Substring(startIndex, endIndex - startIndex);
        }

        private void CreateCategoriesTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Categories (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL UNIQUE,
                    Description TEXT,
                    IsActive BOOLEAN DEFAULT TRUE,
                    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    LastModified TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateLocationsTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Locations (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    Address TEXT,
                    Phone VARCHAR(20),
                    Email VARCHAR(100),
                    IsOnline BOOLEAN DEFAULT FALSE,
                    IsActive BOOLEAN DEFAULT TRUE,
                    Notes TEXT,
                    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    LastModified TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateComponentsTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Components (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    SKU VARCHAR(50) NOT NULL UNIQUE,
                    Name VARCHAR(100) NOT NULL,
                    Description TEXT,
                    CategoryId INT,
                    Price DECIMAL(10,2) DEFAULT 0.00,
                    Cost DECIMAL(10,2) DEFAULT 0.00,
                    IsActive BOOLEAN DEFAULT TRUE,
                    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    LastModified TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateProductsTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Products (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    SKU VARCHAR(50) NOT NULL UNIQUE,
                    Name VARCHAR(100) NOT NULL,
                    Description TEXT,
                    CategoryId INT,
                    Price DECIMAL(10,2) DEFAULT 0.00,
                    Cost DECIMAL(10,2) DEFAULT 0.00,
                    IsActive BOOLEAN DEFAULT TRUE,
                    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    LastModified TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateProductComponentsTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ProductComponents (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    ProductId INT NOT NULL,
                    ComponentId INT NOT NULL,
                    Quantity INT NOT NULL DEFAULT 1,
                    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE KEY unique_product_component (ProductId, ComponentId),
                    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ComponentId) REFERENCES Components(Id) ON DELETE CASCADE
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateLocationInventoryTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS LocationInventory (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    LocationId INT NOT NULL,
                    ItemType ENUM('Product', 'Component') NOT NULL,
                    ItemId INT NOT NULL,
                    CurrentStock INT NOT NULL DEFAULT 0,
                    MinimumStock INT NOT NULL DEFAULT 0,
                    LastModified TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    UNIQUE KEY unique_location_item (LocationId, ItemType, ItemId),
                    FOREIGN KEY (LocationId) REFERENCES Locations(Id) ON DELETE CASCADE,
                    INDEX idx_location (LocationId),
                    INDEX idx_item (ItemType, ItemId)
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateInventoryTransactionsTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS InventoryTransactions (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    TransactionType ENUM('Sale', 'Purchase', 'Transfer', 'Adjustment', 'Return', 'BreakDown', 'Damage', 'Expiry') NOT NULL,
                    ItemType ENUM('Product', 'Component') NOT NULL,
                    ItemId INT NOT NULL,
                    LocationId INT NOT NULL,
                    Quantity INT NOT NULL,
                    Notes TEXT,
                    TransactionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (LocationId) REFERENCES Locations(Id) ON DELETE CASCADE
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateComponentTransformationsTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ComponentTransformations (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    TransformationType ENUM('BreakDown', 'Combine') NOT NULL,
                    SourceComponentId INT NOT NULL,
                    ResultComponentId INT NOT NULL,
                    Quantity INT NOT NULL DEFAULT 1,
                    LocationId INT NOT NULL,
                    Notes TEXT,
                    TransformationDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (SourceComponentId) REFERENCES Components(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ResultComponentId) REFERENCES Components(Id) ON DELETE CASCADE,
                    FOREIGN KEY (LocationId) REFERENCES Locations(Id) ON DELETE CASCADE
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateTransformationItemsTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS TransformationItems (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    TransformationId INT NOT NULL,
                    ComponentId INT NOT NULL,
                    Quantity INT NOT NULL,
                    IsInput BOOLEAN NOT NULL,
                    FOREIGN KEY (TransformationId) REFERENCES ComponentTransformations(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ComponentId) REFERENCES Components(Id) ON DELETE CASCADE
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateEmployeesTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Employees (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    FirstName VARCHAR(50) NOT NULL,
                    LastName VARCHAR(50) NOT NULL,
                    Email VARCHAR(100) UNIQUE,
                    Phone VARCHAR(20),
                    CommissionRate DECIMAL(5,2) DEFAULT 0.00,
                    IsActive BOOLEAN DEFAULT TRUE,
                    HireDate DATE,
                    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    LastModified TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateCustomersTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Customers (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    FirstName VARCHAR(100) NOT NULL,
                    LastName VARCHAR(100) NOT NULL,
                    Email VARCHAR(255),
                    Phone VARCHAR(50),
                    Address TEXT,
                    City VARCHAR(100),
                    State VARCHAR(50),
                    ZipCode VARCHAR(20),
                    Country VARCHAR(100),
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateOrdersTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Orders (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    OrderNumber VARCHAR(50) UNIQUE NOT NULL,
                    CustomerId INT NULL,
                    EmployeeId INT NULL,
                    OrderType ENUM('Sale', 'Transfer', 'Purchase', 'Return') NOT NULL DEFAULT 'Sale',
                    Status ENUM('Pending', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled') NOT NULL DEFAULT 'Pending',
                    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    LastModified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE SET NULL,
                    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE SET NULL
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreateOrderItemsTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS OrderItems (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    OrderId INT NOT NULL,
                    ProductId INT NOT NULL,
                    Quantity INT NOT NULL,
                    UnitPrice DECIMAL(10,2) NOT NULL,
                    TotalPrice DECIMAL(10,2) NOT NULL,
                    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ProductId) REFERENCES Products(Id)
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void CreatePurchaseOrdersTable(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS PurchaseOrders (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    PONumber VARCHAR(50) NOT NULL UNIQUE,
                    PODate DATE NOT NULL,
                    TotalValue DECIMAL(10,2) NOT NULL,
                    Status ENUM('Pending', 'Approved', 'Received', 'Cancelled') DEFAULT 'Pending',
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                )";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void UpdateDatabaseSchema(MySqlConnection connection)
        {
            try
            {
                // Check if TotalPrice column exists in OrderItems table
                var checkColumnSql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'OrderItems' 
                    AND COLUMN_NAME = 'TotalPrice'";

                using var checkCommand = new MySqlCommand(checkColumnSql, connection);
                var columnExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;

                if (!columnExists)
                {
                    // Add TotalPrice column to OrderItems table
                    var addColumnSql = "ALTER TABLE OrderItems ADD COLUMN TotalPrice DECIMAL(10,2) NOT NULL DEFAULT 0.00";
                    using var addCommand = new MySqlCommand(addColumnSql, connection);
                    addCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - schema updates are optional
                System.Diagnostics.Debug.WriteLine($"Schema update error: {ex.Message}");
            }
        }

        private bool IsDatabaseEmpty(MySqlConnection connection)
        {
            try
            {
                // Check if any tables have data by checking a few key tables
                var checkSql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM Categories) as CategoriesCount,
                        (SELECT COUNT(*) FROM Locations) as LocationsCount,
                        (SELECT COUNT(*) FROM Components) as ComponentsCount,
                        (SELECT COUNT(*) FROM Products) as ProductsCount";
                
                using var command = new MySqlCommand(checkSql, connection);
                using var reader = command.ExecuteReader();
                
                if (reader.Read())
                {
                    var categoriesCount = Convert.ToInt32(reader["CategoriesCount"]);
                    var locationsCount = Convert.ToInt32(reader["LocationsCount"]);
                    var componentsCount = Convert.ToInt32(reader["ComponentsCount"]);
                    var productsCount = Convert.ToInt32(reader["ProductsCount"]);
                    
                    // If all tables are empty, the database is new
                    return categoriesCount == 0 && locationsCount == 0 && 
                           componentsCount == 0 && productsCount == 0;
                }
                
                return true; // If we can't read, assume it's empty
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking if database is empty: {ex.Message}");
                return true; // If there's an error, assume it's empty
            }
        }

        private void InsertSampleData(MySqlConnection connection)
        {
            // Insert sample categories
            var categoriesSql = @"
                INSERT IGNORE INTO Categories (Name, Description) VALUES
                ('Necklaces', 'Various types of necklaces'),
                ('Bracelets', 'Wrist jewelry'),
                ('Earrings', 'Ear jewelry'),
                ('Rings', 'Finger jewelry'),
                ('Pendants', 'Necklace pendants')";

            using (var command = new MySqlCommand(categoriesSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // Insert sample locations
            var locationsSql = @"
                INSERT IGNORE INTO Locations (Name, Address, Phone, Email, IsOnline, IsActive) VALUES
                ('Main Store', '123 Main Street, City, State', '555-123-4567', 'main@moonglow.com', FALSE, TRUE),
                ('Online Store', 'Online', '555-000-0000', 'online@moonglow.com', TRUE, TRUE),
                ('Warehouse', '456 Warehouse Ave, City, State', '555-987-6543', 'warehouse@moonglow.com', FALSE, TRUE)";

            using (var command = new MySqlCommand(locationsSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // Insert sample components
            var componentsSql = @"
                INSERT IGNORE INTO Components (SKU, Name, Description, CategoryId, Price, Cost) VALUES
                ('COMP-001', 'Silver Chain 16 inch', '16 inch silver chain', 1, 15.00, 8.00),
                ('COMP-002', 'Gold Chain 18 inch', '18 inch gold chain', 1, 25.00, 12.00),
                ('COMP-003', 'Diamond Pendant', 'Small diamond pendant', 5, 100.00, 60.00),
                ('COMP-004', 'Pearl Bead', 'Natural pearl bead', 1, 5.00, 2.50),
                ('COMP-005', 'Silver Clasp', 'Silver jewelry clasp', 1, 3.00, 1.50)";

            using (var command = new MySqlCommand(componentsSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // Insert sample products
            var productsSql = @"
                INSERT IGNORE INTO Products (SKU, Name, Description, CategoryId, Price, Cost) VALUES
                ('PROD-001', 'Silver Necklace with Diamond', 'Elegant silver necklace with diamond pendant', 1, 150.00, 80.00),
                ('PROD-002', 'Gold Bracelet', 'Classic gold bracelet', 2, 200.00, 120.00),
                ('PROD-003', 'Pearl Earrings', 'Natural pearl earrings', 3, 75.00, 45.00)";

            using (var command = new MySqlCommand(productsSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // Insert product-component relationships
            var productComponentsSql = @"
                INSERT IGNORE INTO ProductComponents (ProductId, ComponentId, Quantity) VALUES
                (1, 1, 1),
                (1, 3, 1),
                (1, 5, 1),
                (2, 2, 1),
                (3, 4, 2)";

            using (var command = new MySqlCommand(productComponentsSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // Insert sample employees
            var employeesSql = @"
                INSERT IGNORE INTO Employees (FirstName, LastName, Email, Phone, CommissionRate, HireDate, IsActive) VALUES
                ('John', 'Smith', 'john.smith@moonglow.com', '555-111-2222', 5.00, '2023-01-15', TRUE),
                ('Jane', 'Doe', 'jane.doe@moonglow.com', '555-333-4444', 7.50, '2023-03-20', TRUE),
                ('Mike', 'Johnson', 'mike.johnson@moonglow.com', '555-555-6666', 4.00, '2023-06-10', TRUE)";

            using (var command = new MySqlCommand(employeesSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // Insert default inventory records for all locations
            var locationInventorySql = @"
                INSERT IGNORE INTO LocationInventory (LocationId, ItemType, ItemId, CurrentStock, MinimumStock)
                SELECT 
                    l.Id as LocationId,
                    'Product' as ItemType,
                    p.Id as ItemId,
                    0 as CurrentStock,
                    0 as MinimumStock
                FROM Locations l
                CROSS JOIN Products p
                WHERE l.IsActive = 1 AND p.IsActive = 1";

            using (var command = new MySqlCommand(locationInventorySql, connection))
            {
                command.ExecuteNonQuery();
            }

            var locationInventoryComponentsSql = @"
                INSERT IGNORE INTO LocationInventory (LocationId, ItemType, ItemId, CurrentStock, MinimumStock)
                SELECT 
                    l.Id as LocationId,
                    'Component' as ItemType,
                    c.Id as ItemId,
                    0 as CurrentStock,
                    0 as MinimumStock
                FROM Locations l
                CROSS JOIN Components c
                WHERE l.IsActive = 1 AND c.IsActive = 1";

            using (var command = new MySqlCommand(locationInventoryComponentsSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // Add some sample inventory
            var sampleInventorySql = @"
                UPDATE LocationInventory SET CurrentStock = 10, MinimumStock = 5 WHERE LocationId = 1 AND ItemType = 'Component' AND ItemId = 1;
                UPDATE LocationInventory SET CurrentStock = 15, MinimumStock = 3 WHERE LocationId = 1 AND ItemType = 'Component' AND ItemId = 2;
                UPDATE LocationInventory SET CurrentStock = 5, MinimumStock = 2 WHERE LocationId = 1 AND ItemType = 'Product' AND ItemId = 1;
                UPDATE LocationInventory SET CurrentStock = 8, MinimumStock = 2 WHERE LocationId = 2 AND ItemType = 'Product' AND ItemId = 1;
                UPDATE LocationInventory SET CurrentStock = 20, MinimumStock = 5 WHERE LocationId = 3 AND ItemType = 'Component' AND ItemId = 4";

            using (var command = new MySqlCommand(sampleInventorySql, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        // Data access methods
        public List<Category> GetAllCategories()
        {
            var categories = new List<Category>();
            var sql = "SELECT Id, Name, Description, IsActive, CreatedDate, LastModified FROM Categories ORDER BY Name";
            
            using var command = CreateCommand(sql);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    IsActive = reader.GetBoolean(3),
                    CreatedDate = reader.GetDateTime(4),
                    LastModified = reader.GetDateTime(5)
                });
            }
            
            return categories;
        }

        public List<Location> GetAllLocations()
        {
            var locations = new List<Location>();
            var sql = "SELECT Id, Name, Address, Phone, Email, IsOnline, IsActive, Notes, CreatedDate, LastModified FROM Locations ORDER BY Name";
            
            using var command = CreateCommand(sql);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                locations.Add(new Location
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Address = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Email = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    IsOnline = reader.GetBoolean(5),
                    IsActive = reader.GetBoolean(6),
                    Notes = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    CreatedDate = reader.GetDateTime(8),
                    LastModified = reader.GetDateTime(9)
                });
            }
            
            return locations;
        }

        public List<Product> GetAllProducts()
        {
            var products = new List<Product>();
            var sql = "SELECT Id, SKU, Name, Description, CategoryId, Price, Cost, IsActive, CreatedDate, LastModified FROM Products ORDER BY Name";
            
            using var command = CreateCommand(sql);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                products.Add(new Product
                {
                    Id = reader.GetInt32(0),
                    SKU = reader.GetString(1),
                    Name = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    CategoryId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                    Price = reader.GetDecimal(5),
                    Cost = reader.GetDecimal(6),
                    IsActive = reader.GetBoolean(7),
                    CreatedDate = reader.GetDateTime(8),
                    LastModified = reader.GetDateTime(9)
                });
            }
            
            return products;
        }

        public List<Component> GetAllComponents()
        {
            var components = new List<Component>();
            var sql = "SELECT Id, SKU, Name, Description, CategoryId, Price, Cost, IsActive, CreatedDate, LastModified FROM Components ORDER BY Name";
            
            using var command = CreateCommand(sql);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                components.Add(new Component
                {
                    Id = reader.GetInt32(0),
                    SKU = reader.GetString(1),
                    Name = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    CategoryId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                    Price = reader.GetDecimal(5),
                    Cost = reader.GetDecimal(6),
                    IsActive = reader.GetBoolean(7),
                    CreatedDate = reader.GetDateTime(8),
                    LastModified = reader.GetDateTime(9)
                });
            }
            
            return components;
        }

        public List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();
            var sql = "SELECT Id, FirstName, LastName, Email, Phone, CommissionRate, IsActive, HireDate, CreatedDate, LastModified FROM Employees ORDER BY LastName, FirstName";
            
            using var command = CreateCommand(sql);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                employees.Add(new Employee
                {
                    Id = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Phone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    CommissionRate = reader.GetDecimal(5),
                    IsActive = reader.GetBoolean(6),
                    HireDate = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7),
                    CreatedDate = reader.GetDateTime(8),
                    LastModified = reader.GetDateTime(9)
                });
            }
            
            return employees;
        }

        public List<Customer> GetAllCustomers()
        {
            var customers = new List<Customer>();
            var sql = "SELECT Id, FirstName, LastName, Email, Phone, Address, City, State, ZipCode, Country, IsActive, CreatedDate FROM Customers ORDER BY LastName, FirstName";
            
            using var command = CreateCommand(sql);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                customers.Add(new Customer
                {
                    Id = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Phone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Address = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    City = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    State = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    ZipCode = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    Country = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    IsActive = reader.GetBoolean(10),
                    CreatedDate = reader.GetDateTime(11)
                });
            }
            
            return customers;
        }

        public void SaveTransaction(InventoryTransaction transaction)
        {
            using var connection = GetConnection();
            using var dbTransaction = connection.BeginTransaction();
            
            try
            {
                // Save the inventory transaction
                var sql = @"
                    INSERT INTO InventoryTransactions 
                    (TransactionType, ItemType, ItemId, LocationId, Quantity, Notes, TransactionDate) 
                    VALUES (@type, @itemType, @itemId, @locationId, @quantity, @notes, @date)";

                using var command = new MySqlCommand(sql, connection);
                command.Transaction = dbTransaction;
                command.Parameters.AddWithValue("@type", transaction.TransactionType.ToString());
                command.Parameters.AddWithValue("@itemType", transaction.ItemType);
                command.Parameters.AddWithValue("@itemId", transaction.ItemId);
                command.Parameters.AddWithValue("@locationId", transaction.LocationId);
                command.Parameters.AddWithValue("@quantity", transaction.Quantity);
                command.Parameters.AddWithValue("@notes", transaction.Notes ?? "");
                command.Parameters.AddWithValue("@date", transaction.TransactionDate);
                
                command.ExecuteNonQuery();

                // Handle component stock updates
                if (transaction.ItemType == "Component" && transaction.LocationId.HasValue)
                {
                    UpdateComponentStock(transaction.ItemId, transaction.LocationId.Value, transaction.Quantity);
                }
                else if (transaction.ItemType == "Product" && transaction.LocationId.HasValue)
                {
                    // For products, we need to handle component consumption/restoration
                    HandleProductTransaction(transaction, connection, dbTransaction);
                }

                dbTransaction.Commit();
            }
            catch
            {
                dbTransaction.Rollback();
                throw;
            }
        }

        private void HandleProductTransaction(InventoryTransaction transaction, MySqlConnection connection, MySqlTransaction dbTransaction)
        {
            // Get the components required for this product
            var sql = @"
                SELECT ComponentId, Quantity 
                FROM ProductComponents 
                WHERE ProductId = @ProductId";
            
            using var command = new MySqlCommand(sql, connection);
            command.Transaction = dbTransaction;
            command.Parameters.AddWithValue("@ProductId", transaction.ItemId);
            
            using var reader = command.ExecuteReader();
            var components = new List<(int ComponentId, int Quantity)>();
            
            while (reader.Read())
            {
                components.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }
            reader.Close();

            // Calculate component quantity changes based on transaction type
            int componentMultiplier = transaction.TransactionType switch
            {
                TransactionType.Sale => -1, // Consume components
                TransactionType.Purchase => 1, // Restore components (if returning assembled product)
                TransactionType.Return => 1, // Restore components
                TransactionType.Adjustment => transaction.Quantity > 0 ? 1 : -1, // Add or remove
                _ => 0 // No component change for other transaction types
            };

            // Update component stock for each required component
            if (transaction.LocationId.HasValue)
            {
                foreach (var (componentId, requiredQuantity) in components)
                {
                    var totalComponentChange = requiredQuantity * transaction.Quantity * componentMultiplier;
                    if (totalComponentChange != 0)
                    {
                        UpdateComponentStock(componentId, transaction.LocationId.Value, totalComponentChange);
                    }
                }
            }
        }

        public Location GetLocationById(int locationId)
        {
            var sql = "SELECT Id, Name, Address, Phone, Email, IsOnline, IsActive, Notes, CreatedDate, LastModified FROM Locations WHERE Id = @id";
            
            using var command = CreateCommand(sql);
            command.Parameters.AddWithValue("@id", locationId);
            using var reader = command.ExecuteReader();
            
            if (reader.Read())
            {
                return new Location
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Address = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Email = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    IsOnline = reader.GetBoolean(5),
                    IsActive = reader.GetBoolean(6),
                    Notes = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    CreatedDate = reader.GetDateTime(8),
                    LastModified = reader.GetDateTime(9)
                };
            }
            
            return null;
        }

        // Get component stock for a specific location
        public int GetComponentStock(int componentId, int locationId)
        {
            var sql = @"
                SELECT CurrentStock 
                FROM LocationInventory 
                WHERE ItemType = 'Component' AND ItemId = @ComponentId AND LocationId = @LocationId";
            
            using var command = CreateCommand(sql);
            command.Parameters.AddWithValue("@ComponentId", componentId);
            command.Parameters.AddWithValue("@LocationId", locationId);
            
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        // Get product stock for a specific location (calculated from components)
        public int GetProductStock(int productId, int locationId)
        {
            var sql = @"
                SELECT pc.ComponentId, pc.Quantity, li.CurrentStock
                FROM ProductComponents pc
                LEFT JOIN LocationInventory li ON li.ItemId = pc.ComponentId 
                    AND li.ItemType = 'Component' 
                    AND li.LocationId = @LocationId
                WHERE pc.ProductId = @ProductId";
            
            using var command = CreateCommand(sql);
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@LocationId", locationId);
            
            using var reader = command.ExecuteReader();
            
            int maxProducts = int.MaxValue;
            while (reader.Read())
            {
                var componentId = reader.GetInt32(0);
                var requiredQuantity = reader.GetInt32(1);
                var availableStock = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                
                // Calculate how many products can be made with this component
                var possibleProducts = availableStock / requiredQuantity;
                maxProducts = Math.Min(maxProducts, possibleProducts);
            }
            
            return maxProducts == int.MaxValue ? 0 : maxProducts;
        }

        // Get minimum stock for a specific item and location
        public int GetMinimumStock(string itemType, int itemId, int locationId)
        {
            var sql = @"
                SELECT MinimumStock 
                FROM LocationInventory 
                WHERE ItemType = @ItemType AND ItemId = @ItemId AND LocationId = @LocationId";
            
            using var command = CreateCommand(sql);
            command.Parameters.AddWithValue("@ItemType", itemType);
            command.Parameters.AddWithValue("@ItemId", itemId);
            command.Parameters.AddWithValue("@LocationId", locationId);
            
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        // Set minimum stock for a specific item and location
        public void SetMinimumStock(string itemType, int itemId, int locationId, int minimumStock)
        {
            var sql = @"
                INSERT INTO LocationInventory (LocationId, ItemType, ItemId, CurrentStock, MinimumStock)
                VALUES (@LocationId, @ItemType, @ItemId, 0, @MinimumStock)
                ON DUPLICATE KEY UPDATE MinimumStock = @MinimumStock";
            
            using var command = CreateCommand(sql);
            command.Parameters.AddWithValue("@LocationId", locationId);
            command.Parameters.AddWithValue("@ItemType", itemType);
            command.Parameters.AddWithValue("@ItemId", itemId);
            command.Parameters.AddWithValue("@MinimumStock", minimumStock);
            
            command.ExecuteNonQuery();
        }

        // Update component stock for a specific location
        public void UpdateComponentStock(int componentId, int locationId, int quantity)
        {
            var sql = @"
                INSERT INTO LocationInventory (LocationId, ItemType, ItemId, CurrentStock, MinimumStock)
                VALUES (@LocationId, 'Component', @ComponentId, @Quantity, 0)
                ON DUPLICATE KEY UPDATE CurrentStock = CurrentStock + @Quantity";
            
            using var command = CreateCommand(sql);
            command.Parameters.AddWithValue("@LocationId", locationId);
            command.Parameters.AddWithValue("@ComponentId", componentId);
            command.Parameters.AddWithValue("@Quantity", quantity);
            
            command.ExecuteNonQuery();
        }

        // Get all products with calculated stock for a specific location
        public List<Product> GetProductsWithStock(int locationId)
        {
            var products = GetAllProducts();
            
            foreach (var product in products)
            {
                product.CurrentStock = GetProductStock(product.Id, locationId);
                product.MinimumStock = GetMinimumStock("Product", product.Id, locationId);
            }
            
            return products;
        }

        // Get all components with stock for a specific location
        public List<Component> GetComponentsWithStock(int locationId)
        {
            var components = GetAllComponents();
            
            foreach (var component in components)
            {
                component.CurrentStock = GetComponentStock(component.Id, locationId);
                component.MinimumStock = GetMinimumStock("Component", component.Id, locationId);
            }
            
            return components;
        }

        // Get all components that make up a product
        public List<ProductComponent> GetProductComponents(int productId)
        {
            var productComponents = new List<ProductComponent>();
            
            var sql = @"
                SELECT pc.ComponentId, pc.Quantity, c.Id, c.Name, c.SKU, c.Description, c.CategoryId, c.Cost, c.Price, c.IsActive, c.CreatedDate, c.LastModified
                FROM ProductComponents pc
                INNER JOIN Components c ON pc.ComponentId = c.Id
                WHERE pc.ProductId = @ProductId
                ORDER BY c.Name";
            
            using var command = CreateCommand(sql);
            command.Parameters.AddWithValue("@ProductId", productId);
            
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                var component = new Component
                {
                    Id = reader.GetInt32(2),
                    Name = reader.GetString(3),
                    SKU = reader.GetString(4),
                    Description = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    CategoryId = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    Cost = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                    Price = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8),
                    IsActive = reader.GetBoolean(9),
                    CreatedDate = reader.GetDateTime(10),
                    LastModified = reader.GetDateTime(11)
                };
                
                productComponents.Add(new ProductComponent
                {
                    ProductId = productId,
                    ComponentId = reader.GetInt32(0),
                    Quantity = reader.GetInt32(1),
                    Component = component
                });
            }
            
            return productComponents;
        }
    }
} 