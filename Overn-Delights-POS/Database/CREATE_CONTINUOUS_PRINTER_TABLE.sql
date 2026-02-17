-- Create table for continuous printer configuration with field coordinates

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContinuousPrinterConfig')
BEGIN
    CREATE TABLE ContinuousPrinterConfig (
        ConfigID INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        PrinterName NVARCHAR(255) NOT NULL,
        PrinterIPAddress NVARCHAR(50) NULL,
        PaperWidth INT NOT NULL DEFAULT 210,  -- mm (A4 = 210mm)
        PaperHeight INT NOT NULL DEFAULT 297, -- mm (A4 = 297mm)
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_ContinuousPrinter_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    PRINT '✓ Created ContinuousPrinterConfig table'
END
ELSE
BEGIN
    PRINT 'ContinuousPrinterConfig table already exists'
END
GO

-- Create table for field coordinates on continuous printer
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContinuousPrinterFields')
BEGIN
    CREATE TABLE ContinuousPrinterFields (
        FieldID INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        FieldName NVARCHAR(50) NOT NULL,  -- e.g., 'InvoiceNumber', 'Date', 'CustomerName'
        XPosition INT NOT NULL,  -- X coordinate in mm
        YPosition INT NOT NULL,  -- Y coordinate in mm
        FontName NVARCHAR(50) NOT NULL DEFAULT 'Arial',
        FontSize INT NOT NULL DEFAULT 10,
        IsBold BIT NOT NULL DEFAULT 0,
        MaxWidth INT NULL,  -- Maximum width in mm (for text wrapping)
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_ContinuousPrinterFields_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    PRINT '✓ Created ContinuousPrinterFields table'
END
ELSE
BEGIN
    PRINT 'ContinuousPrinterFields table already exists'
END
GO

-- Insert default continuous printer config for each branch
INSERT INTO ContinuousPrinterConfig (BranchID, PrinterName, PrinterIPAddress, PaperWidth, PaperHeight, IsActive)
SELECT 
    BranchID,
    '\\NetworkPrinter\ContinuousPrinter' AS PrinterName,
    NULL AS PrinterIPAddress,
    210 AS PaperWidth,   -- A4 width
    297 AS PaperHeight,  -- A4 height
    1 AS IsActive
FROM Branches
WHERE BranchID NOT IN (SELECT BranchID FROM ContinuousPrinterConfig)

PRINT '✓ Added default continuous printer config for all branches'
GO

-- Insert default field positions for Branch 6 (example)
-- Adjust these coordinates based on your actual continuous paper layout
IF NOT EXISTS (SELECT * FROM ContinuousPrinterFields WHERE BranchID = 6)
BEGIN
    INSERT INTO ContinuousPrinterFields (BranchID, FieldName, XPosition, YPosition, FontName, FontSize, IsBold, MaxWidth)
    VALUES
    -- Header
    (6, 'StoreName', 70, 10, 'Arial', 14, 1, 70),
    (6, 'BranchName', 70, 20, 'Arial', 10, 0, 70),
    
    -- Invoice Details
    (6, 'InvoiceNumber', 10, 35, 'Arial', 10, 1, 50),
    (6, 'Date', 120, 35, 'Arial', 10, 0, 50),
    (6, 'Time', 120, 42, 'Arial', 10, 0, 50),
    (6, 'TillNumber', 10, 42, 'Arial', 10, 0, 50),
    (6, 'CashierName', 10, 49, 'Arial', 10, 0, 50),
    
    -- Line Items Header
    (6, 'ItemsHeader', 10, 60, 'Arial', 10, 1, 190),
    
    -- Line Items (dynamic - will be calculated in code)
    (6, 'LineItemStart', 10, 70, 'Arial', 9, 0, 190),
    
    -- Totals
    (6, 'Subtotal', 120, 200, 'Arial', 10, 0, 70),
    (6, 'Tax', 120, 210, 'Arial', 10, 0, 70),
    (6, 'Total', 120, 220, 'Arial', 12, 1, 70),
    
    -- Payment
    (6, 'PaymentMethod', 10, 235, 'Arial', 10, 0, 70),
    (6, 'CashTendered', 10, 245, 'Arial', 10, 0, 70),
    (6, 'Change', 10, 255, 'Arial', 10, 1, 70),
    
    -- Footer
    (6, 'ThankYou', 60, 275, 'Arial', 10, 0, 90)
    
    PRINT '✓ Added default field positions for Branch 6'
END
GO

-- View configuration
PRINT ''
PRINT '=== Continuous Printer Configuration ==='
SELECT 
    c.ConfigID,
    b.BranchName,
    c.PrinterName,
    c.PrinterIPAddress,
    c.PaperWidth,
    c.PaperHeight,
    c.IsActive
FROM ContinuousPrinterConfig c
INNER JOIN Branches b ON c.BranchID = b.BranchID
ORDER BY b.BranchName

PRINT ''
PRINT '=== Field Positions ==='
SELECT 
    f.FieldID,
    b.BranchName,
    f.FieldName,
    f.XPosition,
    f.YPosition,
    f.FontName,
    f.FontSize,
    f.IsBold
FROM ContinuousPrinterFields f
INNER JOIN Branches b ON f.BranchID = b.BranchID
ORDER BY b.BranchName, f.YPosition, f.XPosition

PRINT ''
PRINT 'IMPORTANT: Update printer names and coordinates for your actual setup!'
PRINT 'Example: UPDATE ContinuousPrinterConfig SET PrinterName = ''\\192.168.1.100\KitchenPrinter'' WHERE BranchID = 6'
