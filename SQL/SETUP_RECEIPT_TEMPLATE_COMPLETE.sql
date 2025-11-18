-- Complete Setup for Receipt Template System
-- Run this script FIRST before using POS printing

-- 1. Create ReceiptTemplateConfig table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'ReceiptTemplateConfig') AND type in (N'U'))
BEGIN
    CREATE TABLE ReceiptTemplateConfig (
        ConfigID INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        FieldName NVARCHAR(100) NOT NULL,
        XPosition INT NOT NULL DEFAULT 0,
        YPosition INT NOT NULL DEFAULT 0,
        FontSize INT NOT NULL DEFAULT 8,
        IsBold BIT NOT NULL DEFAULT 0,
        IsEnabled BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_ReceiptTemplate_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    );
    PRINT 'ReceiptTemplateConfig table created';
END
ELSE
BEGIN
    PRINT 'ReceiptTemplateConfig table already exists';
END

-- 2. Add missing columns to Branches table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'BranchPhone')
BEGIN
    ALTER TABLE Branches ADD BranchPhone NVARCHAR(50);
    PRINT 'Added BranchPhone column to Branches table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'BranchEmail')
BEGIN
    ALTER TABLE Branches ADD BranchEmail NVARCHAR(100);
    PRINT 'Added BranchEmail column to Branches table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'RegistrationNumber')
BEGIN
    ALTER TABLE Branches ADD RegistrationNumber NVARCHAR(50);
    PRINT 'Added RegistrationNumber column to Branches table';
END
GO

-- 3. Update branch information for receipts
UPDATE Branches
SET 
    BranchPhone = '031-4079642',
    BranchEmail = 'info@ovendelights.co.za',
    RegistrationNumber = '1987/03793'
WHERE BranchID = 1;  -- Umhlanga

UPDATE Branches
SET 
    BranchPhone = '031-4079642',
    BranchEmail = 'info@ovendelights.co.za',
    RegistrationNumber = '1987/03793'
WHERE BranchID = 2;  -- Ayesha Court

PRINT 'Branch information updated';
GO

-- 4. Insert default receipt template fields for Branch 1
IF NOT EXISTS (SELECT * FROM ReceiptTemplateConfig WHERE BranchID = 1)
BEGIN
    INSERT INTO ReceiptTemplateConfig (BranchID, FieldName, XPosition, YPosition, FontSize, IsBold, IsEnabled)
    VALUES
    -- Header
    (1, 'CompanyName', 10, 10, 12, 1, 1),
    (1, 'CompanyTagline', 10, 30, 8, 0, 1),
    (1, 'CoRegNo', 10, 50, 7, 0, 1),
    (1, 'VATNumber', 10, 65, 7, 0, 1),
    (1, 'ShopNo', 10, 80, 7, 0, 1),
    (1, 'Address', 10, 95, 7, 0, 1),
    (1, 'City', 10, 110, 7, 0, 1),
    (1, 'Phone', 10, 125, 7, 0, 1),
    (1, 'Email', 10, 140, 7, 0, 1),
    (1, 'AccountRef', 10, 160, 7, 1, 1),
    
    -- Customer Info
    (1, 'AccountNo', 10, 185, 8, 0, 1),
    (1, 'CustomerName', 10, 200, 8, 0, 1),
    (1, 'Telephone', 10, 215, 8, 0, 1),
    (1, 'CellNumber', 10, 230, 8, 0, 1),
    (1, 'SpecialRequest', 10, 250, 8, 1, 1),
    
    -- Order Details (Right side)
    (1, 'CakeColour', 450, 50, 8, 0, 1),
    (1, 'CakePicture', 450, 65, 8, 0, 1),
    (1, 'CollectionDate', 450, 80, 8, 0, 1),
    (1, 'CollectionDay', 450, 95, 8, 0, 1),
    (1, 'CollectionTime', 450, 110, 8, 0, 1),
    
    -- Order Header
    (1, 'OrderHeader', 10, 290, 8, 1, 1),
    (1, 'OrderDetails', 10, 305, 8, 0, 1),
    
    -- Items
    (1, 'ItemHeader', 10, 330, 8, 1, 1),
    (1, 'ItemLine1', 10, 345, 8, 0, 1),
    
    -- Message
    (1, 'Message', 10, 380, 10, 1, 1),
    
    -- Terms (Bottom left)
    (1, 'Terms', 10, 650, 7, 0, 1),
    (1, 'Terms2', 10, 665, 7, 0, 1),
    
    -- Totals (Bottom right)
    (1, 'InvoiceTotal', 450, 650, 9, 1, 1),
    (1, 'DepositPaid', 450, 670, 9, 0, 1),
    (1, 'BalanceOwing', 450, 690, 9, 1, 1);
    
    PRINT 'Default receipt template created for Branch 1';
END

-- 5. Insert default receipt template fields for Branch 2
IF NOT EXISTS (SELECT * FROM ReceiptTemplateConfig WHERE BranchID = 2)
BEGIN
    INSERT INTO ReceiptTemplateConfig (BranchID, FieldName, XPosition, YPosition, FontSize, IsBold, IsEnabled)
    SELECT 2, FieldName, XPosition, YPosition, FontSize, IsBold, IsEnabled
    FROM ReceiptTemplateConfig
    WHERE BranchID = 1;
    
    PRINT 'Default receipt template created for Branch 2';
END
GO

-- 6. Verify ContinuousPrinterConfig table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContinuousPrinterConfig')
BEGIN
    PRINT '';
    PRINT 'WARNING: ContinuousPrinterConfig table does not exist!';
    PRINT 'Please run CREATE_CONTINUOUS_PRINTER_CONFIG.sql to create printer config table';
    PRINT '';
END
ELSE
BEGIN
    PRINT 'ContinuousPrinterConfig table exists';
END

-- 7. View current configuration
PRINT '';
PRINT '========================================';
PRINT 'CURRENT RECEIPT TEMPLATE CONFIGURATION';
PRINT '========================================';

SELECT 
    b.BranchName,
    COUNT(r.ConfigID) AS FieldCount,
    b.BranchPhone,
    b.BranchEmail,
    b.RegistrationNumber
FROM Branches b
LEFT JOIN ReceiptTemplateConfig r ON b.BranchID = r.BranchID
WHERE b.BranchID IN (1, 2)
GROUP BY b.BranchName, b.BranchPhone, b.BranchEmail, b.RegistrationNumber;

PRINT '';
PRINT 'Receipt template fields per branch:';
SELECT 
    b.BranchName,
    r.FieldName,
    r.XPosition,
    r.YPosition,
    r.FontSize,
    r.IsBold,
    r.IsEnabled
FROM ReceiptTemplateConfig r
INNER JOIN Branches b ON r.BranchID = b.BranchID
ORDER BY b.BranchName, r.YPosition;

PRINT '';
PRINT '========================================';
PRINT 'SETUP COMPLETE!';
PRINT '========================================';
PRINT 'Next steps:';
PRINT '1. Run CREATE_CONTINUOUS_PRINTER_CONFIG.sql (if not done)';
PRINT '2. Run SETUP_POS_PRINTER.sql to configure printer';
PRINT '3. Open ERP Receipt Template Designer to adjust positions';
PRINT '4. Test print from POS';
PRINT '========================================';
