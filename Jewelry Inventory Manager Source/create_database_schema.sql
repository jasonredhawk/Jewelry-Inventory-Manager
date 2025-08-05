-- Create the database
CREATE DATABASE IF NOT EXISTS `moonglow_db`;
USE `moonglow_db`;

-- Create Categories table
CREATE TABLE IF NOT EXISTS `Categories` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(100) NOT NULL UNIQUE,
    `Description` TEXT,
    `IsActive` BOOLEAN DEFAULT TRUE,
    `CreatedDate` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `LastModified` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Create Locations table
CREATE TABLE IF NOT EXISTS `Locations` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(100) NOT NULL,
    `Address` TEXT,
    `Phone` VARCHAR(20),
    `Email` VARCHAR(100),
    `IsOnline` BOOLEAN DEFAULT FALSE,
    `IsActive` BOOLEAN DEFAULT TRUE,
    `Notes` TEXT,
    `CreatedDate` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `LastModified` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Create Components table
CREATE TABLE IF NOT EXISTS `Components` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `SKU` VARCHAR(50) NOT NULL UNIQUE,
    `Name` VARCHAR(100) NOT NULL,
    `Description` TEXT,
    `CategoryId` INT,
    `Price` DECIMAL(10,2) DEFAULT 0.00,
    `Cost` DECIMAL(10,2) DEFAULT 0.00,
    `IsActive` BOOLEAN DEFAULT TRUE,
    `CreatedDate` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `LastModified` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (`CategoryId`) REFERENCES `Categories`(`Id`) ON DELETE SET NULL
);

-- Create Products table
CREATE TABLE IF NOT EXISTS `Products` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `SKU` VARCHAR(50) NOT NULL UNIQUE,
    `Name` VARCHAR(100) NOT NULL,
    `Description` TEXT,
    `CategoryId` INT,
    `Price` DECIMAL(10,2) DEFAULT 0.00,
    `Cost` DECIMAL(10,2) DEFAULT 0.00,
    `IsActive` BOOLEAN DEFAULT TRUE,
    `CreatedDate` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `LastModified` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (`CategoryId`) REFERENCES `Categories`(`Id`) ON DELETE SET NULL
);

-- Create ProductComponents table (for product-component relationships)
CREATE TABLE IF NOT EXISTS `ProductComponents` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `ProductId` INT NOT NULL,
    `ComponentId` INT NOT NULL,
    `Quantity` INT NOT NULL DEFAULT 1,
    `CreatedDate` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY `unique_product_component` (`ProductId`, `ComponentId`),
    FOREIGN KEY (`ProductId`) REFERENCES `Products`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`ComponentId`) REFERENCES `Components`(`Id`) ON DELETE CASCADE
);

-- Create LocationInventory table for location-based inventory tracking
CREATE TABLE IF NOT EXISTS `LocationInventory` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `LocationId` INT NOT NULL,
    `ItemType` ENUM('Product', 'Component') NOT NULL,
    `ItemId` INT NOT NULL,
    `CurrentStock` INT NOT NULL DEFAULT 0,
    `MinimumStock` INT NOT NULL DEFAULT 0,
    `LastModified` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `unique_location_item` (`LocationId`, `ItemType`, `ItemId`),
    FOREIGN KEY (`LocationId`) REFERENCES `Locations`(`Id`) ON DELETE CASCADE,
    INDEX `idx_location` (`LocationId`),
    INDEX `idx_item` (`ItemType`, `ItemId`)
);

-- Create InventoryTransactions table
CREATE TABLE IF NOT EXISTS `InventoryTransactions` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TransactionType` ENUM('Sale', 'Purchase', 'Transfer', 'Adjustment', 'Return', 'BreakDown', 'Damage', 'Expiry') NOT NULL,
    `ItemType` ENUM('Product', 'Component') NOT NULL,
    `ItemId` INT NOT NULL,
    `LocationId` INT NOT NULL,
    `Quantity` INT NOT NULL,
    `Notes` TEXT,
    `TransactionDate` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (`LocationId`) REFERENCES `Locations`(`Id`) ON DELETE CASCADE
);

-- Create ComponentTransformations table
CREATE TABLE IF NOT EXISTS `ComponentTransformations` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TransformationType` ENUM('BreakDown', 'Combine') NOT NULL,
    `SourceComponentId` INT NOT NULL,
    `ResultComponentId` INT NOT NULL,
    `Quantity` INT NOT NULL DEFAULT 1,
    `LocationId` INT NOT NULL,
    `Notes` TEXT,
    `TransformationDate` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (`SourceComponentId`) REFERENCES `Components`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`ResultComponentId`) REFERENCES `Components`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`LocationId`) REFERENCES `Locations`(`Id`) ON DELETE CASCADE
);

-- Create TransformationItems table
CREATE TABLE IF NOT EXISTS `TransformationItems` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TransformationId` INT NOT NULL,
    `ComponentId` INT NOT NULL,
    `Quantity` INT NOT NULL,
    `IsInput` BOOLEAN NOT NULL,
    FOREIGN KEY (`TransformationId`) REFERENCES `ComponentTransformations`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`ComponentId`) REFERENCES `Components`(`Id`) ON DELETE CASCADE
);

-- Create Employees table
CREATE TABLE IF NOT EXISTS `Employees` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `FirstName` VARCHAR(50) NOT NULL,
    `LastName` VARCHAR(50) NOT NULL,
    `Email` VARCHAR(100) UNIQUE,
    `Phone` VARCHAR(20),
    `CommissionRate` DECIMAL(5,2) DEFAULT 0.00,
    `IsActive` BOOLEAN DEFAULT TRUE,
    `HireDate` DATE,
    `CreatedDate` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `LastModified` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Insert sample data for testing

-- Insert sample categories
INSERT INTO `Categories` (`Name`, `Description`) VALUES
('Necklaces', 'Various types of necklaces'),
('Bracelets', 'Wrist jewelry'),
('Earrings', 'Ear jewelry'),
('Rings', 'Finger jewelry'),
('Pendants', 'Necklace pendants');

-- Insert sample locations
INSERT INTO `Locations` (`Name`, `Address`, `Phone`, `Email`, `IsOnline`, `IsActive`) VALUES
('Main Store', '123 Main Street, City, State', '555-123-4567', 'main@moonglow.com', FALSE, TRUE),
('Online Store', 'Online', '555-000-0000', 'online@moonglow.com', TRUE, TRUE),
('Warehouse', '456 Warehouse Ave, City, State', '555-987-6543', 'warehouse@moonglow.com', FALSE, TRUE);

-- Insert sample components
INSERT INTO `Components` (`SKU`, `Name`, `Description`, `CategoryId`, `Price`, `Cost`) VALUES
('COMP-001', 'Silver Chain 16"', '16 inch silver chain', 1, 15.00, 8.00),
('COMP-002', 'Gold Chain 18"', '18 inch gold chain', 1, 25.00, 12.00),
('COMP-003', 'Diamond Pendant', 'Small diamond pendant', 5, 100.00, 60.00),
('COMP-004', 'Pearl Bead', 'Natural pearl bead', 1, 5.00, 2.50),
('COMP-005', 'Silver Clasp', 'Silver jewelry clasp', 1, 3.00, 1.50);

-- Insert sample products
INSERT INTO `Products` (`SKU`, `Name`, `Description`, `CategoryId`, `Price`, `Cost`) VALUES
('PROD-001', 'Silver Necklace with Diamond', 'Elegant silver necklace with diamond pendant', 1, 150.00, 80.00),
('PROD-002', 'Gold Bracelet', 'Classic gold bracelet', 2, 200.00, 120.00),
('PROD-003', 'Pearl Earrings', 'Natural pearl earrings', 3, 75.00, 45.00);

-- Insert product-component relationships
INSERT INTO `ProductComponents` (`ProductId`, `ComponentId`, `Quantity`) VALUES
(1, 1, 1),  -- Silver Necklace uses Silver Chain
(1, 3, 1),  -- Silver Necklace uses Diamond Pendant
(1, 5, 1),  -- Silver Necklace uses Silver Clasp
(2, 2, 1),  -- Gold Bracelet uses Gold Chain
(3, 4, 2);  -- Pearl Earrings uses 2 Pearl Beads

-- Insert sample employees
INSERT INTO `Employees` (`FirstName`, `LastName`, `Email`, `Phone`, `CommissionRate`, `HireDate`, `IsActive`) VALUES
('John', 'Smith', 'john.smith@moonglow.com', '555-111-2222', 5.00, '2023-01-15', TRUE),
('Jane', 'Doe', 'jane.doe@moonglow.com', '555-333-4444', 7.50, '2023-03-20', TRUE),
('Mike', 'Johnson', 'mike.johnson@moonglow.com', '555-555-6666', 4.00, '2023-06-10', TRUE);

-- Insert default inventory records for all locations
INSERT INTO `LocationInventory` (`LocationId`, `ItemType`, `ItemId`, `CurrentStock`, `MinimumStock`)
SELECT 
    l.Id as LocationId,
    'Product' as ItemType,
    p.Id as ItemId,
    0 as CurrentStock,
    0 as MinimumStock
FROM Locations l
CROSS JOIN Products p
WHERE l.IsActive = 1 AND p.IsActive = 1;

INSERT INTO `LocationInventory` (`LocationId`, `ItemType`, `ItemId`, `CurrentStock`, `MinimumStock`)
SELECT 
    l.Id as LocationId,
    'Component' as ItemType,
    c.Id as ItemId,
    0 as CurrentStock,
    0 as MinimumStock
FROM Locations l
CROSS JOIN Components c
WHERE l.IsActive = 1 AND c.IsActive = 1;

-- Add some sample inventory
UPDATE `LocationInventory` SET `CurrentStock` = 10, `MinimumStock` = 5 WHERE `LocationId` = 1 AND `ItemType` = 'Component' AND `ItemId` = 1;
UPDATE `LocationInventory` SET `CurrentStock` = 15, `MinimumStock` = 3 WHERE `LocationId` = 1 AND `ItemType` = 'Component' AND `ItemId` = 2;
UPDATE `LocationInventory` SET `CurrentStock` = 5, `MinimumStock` = 2 WHERE `LocationId` = 1 AND `ItemType` = 'Product' AND `ItemId` = 1;
UPDATE `LocationInventory` SET `CurrentStock` = 8, `MinimumStock` = 2 WHERE `LocationId` = 2 AND `ItemType` = 'Product' AND `ItemId` = 1;
UPDATE `LocationInventory` SET `CurrentStock` = 20, `MinimumStock` = 5 WHERE `LocationId` = 3 AND `ItemType` = 'Component' AND `ItemId` = 4; 