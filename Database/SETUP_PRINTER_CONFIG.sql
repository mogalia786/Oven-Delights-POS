-- Setup Printer Configuration for POS
-- This configures the slip printer and optional network printer for each branch

-- Check if PrinterConfig table exists, if not create it
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PrinterConfig')
BEGIN
    CREATE TABLE PrinterConfig (
        PrinterConfigID INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        PrinterName NVARCHAR(255) NOT NULL,
        PrinterIPAddress NVARCHAR(50) NULL,
        IsNetworkPrinter BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PrinterConfig_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    PRINT 'PrinterConfig table created'
END

-- Insert default printer configurations for each branch
-- Replace 'Microsoft Print to PDF' with your actual printer name

-- Get all branches and insert default printer config
INSERT INTO PrinterConfig (BranchID, PrinterName, IsNetworkPrinter, IsActive)
SELECT 
    BranchID,
    'Microsoft Print to PDF' AS PrinterName,  -- Change this to your actual slip printer name
    0 AS IsNetworkPrinter,
    1 AS IsActive
FROM Branches
WHERE BranchID NOT IN (SELECT BranchID FROM PrinterConfig WHERE IsNetworkPrinter = 0)

PRINT 'Default slip printers configured for all branches'

-- Optional: Add network printer configuration (for kitchen/production)
-- Uncomment and modify the lines below to add network printers

/*
-- Example: Add network printer for Branch 1
INSERT INTO PrinterConfig (BranchID, PrinterName, PrinterIPAddress, IsNetworkPrinter, IsActive)
VALUES (1, '\\192.168.1.100\KitchenPrinter', '192.168.1.100', 1, 1)

-- Example: Add network printer for Branch 2
INSERT INTO PrinterConfig (BranchID, PrinterName, PrinterIPAddress, IsNetworkPrinter, IsActive)
VALUES (2, '\\192.168.1.101\KitchenPrinter', '192.168.1.101', 1, 1)
*/

-- View current printer configuration
SELECT 
    pc.PrinterConfigID,
    b.BranchName,
    pc.PrinterName,
    pc.PrinterIPAddress,
    CASE WHEN pc.IsNetworkPrinter = 1 THEN 'Network Printer' ELSE 'Slip Printer' END AS PrinterType,
    CASE WHEN pc.IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS Status
FROM PrinterConfig pc
INNER JOIN Branches b ON b.BranchID = pc.BranchID
ORDER BY b.BranchName, pc.IsNetworkPrinter

GO
