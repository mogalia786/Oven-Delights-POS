-- Create Receipt Template Configuration Tables
-- This allows dynamic positioning of receipt fields without code changes

-- 1. Receipt Template Master Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReceiptTemplates')
BEGIN
    CREATE TABLE dbo.ReceiptTemplates (
        TemplateID INT IDENTITY(1,1) PRIMARY KEY,
        TemplateName NVARCHAR(100) NOT NULL,
        TemplateType NVARCHAR(50) NOT NULL, -- 'SALE', 'ORDER', 'RETURN'
        BranchID INT NULL, -- NULL = default for all branches
        PaperWidth INT DEFAULT 80, -- mm
        LeftMargin INT DEFAULT 10, -- pixels
        TopMargin INT DEFAULT 10, -- pixels
        LineHeight INT DEFAULT 16, -- pixels
        FontName NVARCHAR(50) DEFAULT 'Courier New',
        FontSize INT DEFAULT 9,
        IsActive BIT DEFAULT 1,
        IsDefault BIT DEFAULT 0,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT UK_ReceiptTemplate UNIQUE (TemplateName, BranchID)
    );
    PRINT 'ReceiptTemplates table created';
END

-- 2. Receipt Field Definitions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReceiptFields')
BEGIN
    CREATE TABLE dbo.ReceiptFields (
        FieldID INT IDENTITY(1,1) PRIMARY KEY,
        TemplateID INT NOT NULL,
        FieldName NVARCHAR(100) NOT NULL, -- 'BranchName', 'InvoiceNumber', 'ItemsHeader', etc.
        FieldLabel NVARCHAR(200), -- Display text (e.g., "Invoice No:", "Total:")
        FieldType NVARCHAR(50) NOT NULL, -- 'TEXT', 'HEADER', 'LINE', 'ITEMS', 'SEPARATOR'
        XPosition INT DEFAULT 0, -- Horizontal position (pixels from left margin)
        YPosition INT DEFAULT 0, -- Vertical position (pixels from top)
        Width INT DEFAULT 400, -- Field width in pixels
        FontName NVARCHAR(50) DEFAULT 'Courier New',
        FontSize INT DEFAULT 9,
        FontBold BIT DEFAULT 0,
        FontItalic BIT DEFAULT 0,
        Alignment NVARCHAR(20) DEFAULT 'LEFT', -- 'LEFT', 'CENTER', 'RIGHT'
        DisplayOrder INT DEFAULT 0, -- Order in which fields appear
        IsVisible BIT DEFAULT 1,
        FormatString NVARCHAR(50), -- e.g., 'N2' for currency, 'yyyy/MM/dd' for dates
        MaxLength INT, -- Truncate text if longer
        WordWrap BIT DEFAULT 0,
        CONSTRAINT FK_ReceiptField_Template FOREIGN KEY (TemplateID) REFERENCES ReceiptTemplates(TemplateID) ON DELETE CASCADE
    );
    PRINT 'ReceiptFields table created';
END

-- 3. Insert Default Sale Receipt Template
IF NOT EXISTS (SELECT * FROM ReceiptTemplates WHERE TemplateName = 'Default Sale Receipt')
BEGIN
    INSERT INTO ReceiptTemplates (TemplateName, TemplateType, PaperWidth, LeftMargin, TopMargin, LineHeight, FontName, FontSize, IsActive, IsDefault)
    VALUES ('Default Sale Receipt', 'SALE', 80, 10, 10, 16, 'Courier New', 9, 1, 1);
    
    DECLARE @TemplateID INT = SCOPE_IDENTITY();
    
    -- Insert field definitions for sale receipt
    INSERT INTO ReceiptFields (TemplateID, FieldName, FieldLabel, FieldType, YPosition, FontSize, FontBold, DisplayOrder, IsVisible)
    VALUES
    -- Header
    (@TemplateID, 'CompanyName', 'OVEN DELIGHTS', 'HEADER', 10, 12, 1, 1, 1),
    (@TemplateID, 'CompanyTagline', 'YOUR TRUSTED FAMILY BAKERY', 'TEXT', 35, 9, 0, 2, 1),
    (@TemplateID, 'BranchName', NULL, 'TEXT', 55, 10, 1, 3, 1),
    (@TemplateID, 'BranchAddress', NULL, 'TEXT', 73, 9, 0, 4, 1),
    (@TemplateID, 'BranchPhone', 'Tel:', 'TEXT', 89, 9, 0, 5, 1),
    (@TemplateID, 'BranchEmail', 'Email:', 'TEXT', 105, 9, 0, 6, 1),
    (@TemplateID, 'RegistrationNumber', 'Co Reg No:', 'TEXT', 121, 9, 0, 7, 1),
    (@TemplateID, 'Separator1', NULL, 'SEPARATOR', 137, 9, 0, 8, 1),
    
    -- Receipt Details
    (@TemplateID, 'ReceiptTitle', 'SALE RECEIPT', 'HEADER', 157, 10, 1, 9, 1),
    (@TemplateID, 'InvoiceNumber', 'Invoice:', 'TEXT', 177, 9, 0, 10, 1),
    (@TemplateID, 'SaleDate', 'Date:', 'TEXT', 193, 9, 0, 11, 1),
    (@TemplateID, 'Cashier', 'Cashier:', 'TEXT', 209, 9, 0, 12, 1),
    (@TemplateID, 'Separator2', NULL, 'SEPARATOR', 229, 9, 0, 13, 1),
    
    -- Items Header
    (@TemplateID, 'ItemsHeader', 'Item                    Qty  Price   Total', 'TEXT', 249, 9, 0, 14, 1),
    (@TemplateID, 'Separator3', NULL, 'SEPARATOR', 265, 9, 0, 15, 1),
    (@TemplateID, 'ItemsList', NULL, 'ITEMS', 283, 9, 0, 16, 1),
    
    -- Totals (Y position will be dynamic based on items)
    (@TemplateID, 'Separator4', NULL, 'SEPARATOR', 0, 9, 0, 17, 1),
    (@TemplateID, 'TotalAmount', 'TOTAL:', 'TEXT', 0, 10, 1, 18, 1),
    (@TemplateID, 'PaymentMethod', 'Payment:', 'TEXT', 0, 9, 0, 19, 1),
    (@TemplateID, 'Separator5', NULL, 'SEPARATOR', 0, 9, 0, 20, 1),
    
    -- Footer
    (@TemplateID, 'ThankYou1', 'Thank you for your business!', 'TEXT', 0, 9, 0, 21, 1),
    (@TemplateID, 'ThankYou2', 'Please visit us again!', 'TEXT', 0, 9, 0, 22, 1);
    
    PRINT 'Default Sale Receipt template created';
END

-- 4. Insert Default Order Receipt Template
IF NOT EXISTS (SELECT * FROM ReceiptTemplates WHERE TemplateName = 'Default Order Receipt')
BEGIN
    INSERT INTO ReceiptTemplates (TemplateName, TemplateType, PaperWidth, LeftMargin, TopMargin, LineHeight, FontName, FontSize, IsActive, IsDefault)
    VALUES ('Default Order Receipt', 'ORDER', 80, 10, 10, 16, 'Courier New', 9, 1, 1);
    
    DECLARE @OrderTemplateID INT = SCOPE_IDENTITY();
    
    -- Insert field definitions for order receipt
    INSERT INTO ReceiptFields (TemplateID, FieldName, FieldLabel, FieldType, YPosition, FontSize, FontBold, DisplayOrder, IsVisible)
    VALUES
    -- Header
    (@OrderTemplateID, 'CompanyName', 'OVEN DELIGHTS', 'HEADER', 10, 12, 1, 1, 1),
    (@OrderTemplateID, 'CompanyTagline', 'YOUR TRUSTED FAMILY BAKERY', 'TEXT', 35, 9, 0, 2, 1),
    (@OrderTemplateID, 'BranchName', NULL, 'TEXT', 55, 10, 1, 3, 1),
    (@OrderTemplateID, 'BranchAddress', NULL, 'TEXT', 73, 9, 0, 4, 1),
    (@OrderTemplateID, 'BranchPhone', 'Tel:', 'TEXT', 89, 9, 0, 5, 1),
    (@OrderTemplateID, 'BranchEmail', 'Email:', 'TEXT', 105, 9, 0, 6, 1),
    (@OrderTemplateID, 'RegistrationNumber', 'Co Reg No:', 'TEXT', 121, 9, 0, 7, 1),
    (@OrderTemplateID, 'Separator1', NULL, 'SEPARATOR', 137, 9, 0, 8, 1),
    
    -- Order Details
    (@OrderTemplateID, 'OrderTitle', 'CUSTOM ORDER', 'HEADER', 157, 10, 1, 9, 1),
    (@OrderTemplateID, 'OrderNumber', 'Order No:', 'TEXT', 177, 9, 0, 10, 1),
    (@OrderTemplateID, 'OrderDate', 'Date:', 'TEXT', 193, 9, 0, 11, 1),
    (@OrderTemplateID, 'Separator2', NULL, 'SEPARATOR', 213, 9, 0, 12, 1),
    
    -- Customer Info
    (@OrderTemplateID, 'CustomerName', 'Customer:', 'TEXT', 233, 9, 0, 13, 1),
    (@OrderTemplateID, 'CustomerPhone', 'Tel:', 'TEXT', 249, 9, 0, 14, 1),
    (@OrderTemplateID, 'CustomerCell', 'Cell:', 'TEXT', 265, 9, 0, 15, 1),
    (@OrderTemplateID, 'Separator3', NULL, 'SEPARATOR', 285, 9, 0, 16, 1),
    
    -- Order Specifics
    (@OrderTemplateID, 'CakeColour', 'Cake Colour:', 'TEXT', 305, 9, 0, 17, 1),
    (@OrderTemplateID, 'CollectionDate', 'Collection:', 'TEXT', 321, 9, 0, 18, 1),
    (@OrderTemplateID, 'CollectionTime', 'Time:', 'TEXT', 337, 9, 0, 19, 1),
    (@OrderTemplateID, 'Separator4', NULL, 'SEPARATOR', 357, 9, 0, 20, 1),
    
    -- Special Request
    (@OrderTemplateID, 'SpecialRequestLabel', 'Special Request:', 'TEXT', 377, 9, 1, 21, 1),
    (@OrderTemplateID, 'SpecialRequest', NULL, 'TEXT', 393, 9, 0, 22, 1),
    (@OrderTemplateID, 'Separator5', NULL, 'SEPARATOR', 0, 9, 0, 23, 1),
    
    -- Items
    (@OrderTemplateID, 'ItemsHeader', 'Item              Qty  Unit Price  Total', 'TEXT', 0, 9, 0, 24, 1),
    (@OrderTemplateID, 'Separator6', NULL, 'SEPARATOR', 0, 9, 0, 25, 1),
    (@OrderTemplateID, 'ItemsList', NULL, 'ITEMS', 0, 9, 0, 26, 1),
    (@OrderTemplateID, 'Separator7', NULL, 'SEPARATOR', 0, 9, 0, 27, 1),
    
    -- Totals
    (@OrderTemplateID, 'InvoiceTotal', 'Invoice Total:', 'TEXT', 0, 10, 1, 28, 1),
    (@OrderTemplateID, 'DepositPaid', 'Deposit Paid:', 'TEXT', 0, 9, 0, 29, 1),
    (@OrderTemplateID, 'BalanceOwing', 'Balance Owing:', 'TEXT', 0, 10, 1, 30, 1),
    (@OrderTemplateID, 'Separator8', NULL, 'SEPARATOR', 0, 9, 0, 31, 1),
    
    -- Footer
    (@OrderTemplateID, 'ThankYou1', 'Thank you for your business!', 'TEXT', 0, 9, 0, 32, 1),
    (@OrderTemplateID, 'ThankYou2', 'Please visit us again!', 'TEXT', 0, 9, 0, 33, 1);
    
    PRINT 'Default Order Receipt template created';
END

-- 5. View current templates
SELECT 
    t.TemplateID,
    t.TemplateName,
    t.TemplateType,
    b.BranchName,
    t.PaperWidth,
    t.LeftMargin,
    t.LineHeight,
    t.IsActive,
    t.IsDefault,
    COUNT(f.FieldID) AS FieldCount
FROM ReceiptTemplates t
LEFT JOIN Branches b ON t.BranchID = b.BranchID
LEFT JOIN ReceiptFields f ON t.TemplateID = f.TemplateID
GROUP BY t.TemplateID, t.TemplateName, t.TemplateType, b.BranchName, t.PaperWidth, t.LeftMargin, t.LineHeight, t.IsActive, t.IsDefault
ORDER BY t.TemplateType, t.TemplateName;

PRINT '';
PRINT '========================================';
PRINT 'Receipt Template System Created!';
PRINT '========================================';
PRINT 'You can now adjust field positions in the database';
PRINT 'without recompiling the POS application.';
PRINT '';
PRINT 'To adjust field positions:';
PRINT '1. Query ReceiptFields table';
PRINT '2. Update YPosition values';
PRINT '3. Changes take effect immediately';
PRINT '========================================';
