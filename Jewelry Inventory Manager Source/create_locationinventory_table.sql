-- Create LocationInventory table for location-based inventory tracking
CREATE TABLE IF NOT EXISTS `moonglow_db`.`LocationInventory` (
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

-- Insert default inventory records for existing locations
-- This will create inventory records for all existing products and components at all locations
INSERT IGNORE INTO LocationInventory (LocationId, ItemType, ItemId, CurrentStock, MinimumStock)
SELECT 
    l.Id as LocationId,
    'Product' as ItemType,
    p.Id as ItemId,
    0 as CurrentStock,
    0 as MinimumStock
FROM Locations l
CROSS JOIN Products p
WHERE l.IsActive = 1 AND p.IsActive = 1;

INSERT IGNORE INTO LocationInventory (LocationId, ItemType, ItemId, CurrentStock, MinimumStock)
SELECT 
    l.Id as LocationId,
    'Component' as ItemType,
    c.Id as ItemId,
    0 as CurrentStock,
    0 as MinimumStock
FROM Locations l
CROSS JOIN Components c
WHERE l.IsActive = 1 AND c.IsActive = 1; 