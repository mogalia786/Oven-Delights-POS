-- Setup POS Continuous Printer Configuration
-- Run this script to configure printers for each branch

-- 1. Verify ContinuousPrinterConfig table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContinuousPrinterConfig')
BEGIN
    PRINT 'ERROR: ContinuousPrinterConfig table does not exist!'
    PRINT 'Please run CREATE_CONTINUOUS_PRINTER_CONFIG.sql first'
    RETURN
END

-- 2. Check current printer configurations
SELECT 
    c.ConfigID,
    b.BranchName,
    c.PrinterName,
    c.PrinterPath,
    c.PaperWidth,
    c.IsActive,
    c.CreatedDate
FROM ContinuousPrinterConfig c
INNER JOIN Branches b ON c.BranchID = b.BranchID
ORDER BY b.BranchName

-- 3. Add printer configuration for each branch
-- IMPORTANT: Replace printer name and path with actual values from Windows

-- Example for Umhlanga branch (BranchID = 1)
IF NOT EXISTS (SELECT * FROM ContinuousPrinterConfig WHERE BranchID = 1)
BEGIN
    INSERT INTO ContinuousPrinterConfig (
        BranchID, 
        PrinterName, 
        PrinterPath,
        PaperWidth,
        IsActive,
        CreatedDate
    )
    VALUES (
        1,  -- Umhlanga BranchID
        'Star TSP143',  -- Replace with your actual printer name
        '\\SERVER\Star_TSP143',  -- Replace with network path or local printer name
        80,  -- 80mm paper width
        1,
        GETDATE()
    )
    PRINT 'Printer configured for Umhlanga branch'
END

-- Example for Ayesha Court branch (BranchID = 2)
IF NOT EXISTS (SELECT * FROM ContinuousPrinterConfig WHERE BranchID = 2)
BEGIN
    INSERT INTO ContinuousPrinterConfig (
        BranchID, 
        PrinterName, 
        PrinterPath,
        PaperWidth,
        IsActive,
        CreatedDate
    )
    VALUES (
        2,  -- Ayesha Court BranchID
        'Star TSP143',  -- Replace with your actual printer name
        '\\SERVER\Star_TSP143',  -- Replace with network path or local printer name
        80,
        1,
        GETDATE()
    )
    PRINT 'Printer configured for Ayesha Court branch'
END

-- 4. Verify Branches table has required columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'BranchPhone')
BEGIN
    PRINT 'WARNING: Branches table missing BranchPhone column'
    ALTER TABLE Branches ADD BranchPhone NVARCHAR(50)
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'BranchEmail')
BEGIN
    PRINT 'WARNING: Branches table missing BranchEmail column'
    ALTER TABLE Branches ADD BranchEmail NVARCHAR(100)
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'RegistrationNumber')
BEGIN
    PRINT 'WARNING: Branches table missing RegistrationNumber column'
    ALTER TABLE Branches ADD RegistrationNumber NVARCHAR(50)
END

-- 5. Update branch information for receipts
UPDATE Branches
SET 
    BranchPhone = '031-4079642',
    BranchEmail = 'info@ovendelights.co.za',
    RegistrationNumber = '1987/03793'
WHERE BranchID = 1  -- Umhlanga

UPDATE Branches
SET 
    BranchPhone = '031-4079642',
    BranchEmail = 'info@ovendelights.co.za',
    RegistrationNumber = '1987/03793'
WHERE BranchID = 2  -- Ayesha Court

-- 6. Test query - Get printer for a branch
DECLARE @BranchID INT = 1
SELECT TOP 1 
    PrinterName,
    PrinterPath,
    PaperWidth
FROM ContinuousPrinterConfig 
WHERE BranchID = @BranchID AND IsActive = 1

-- 7. How to find your printer name in Windows:
PRINT ''
PRINT '========================================='
PRINT 'HOW TO FIND YOUR PRINTER NAME:'
PRINT '========================================='
PRINT '1. Open Control Panel'
PRINT '2. Go to Devices and Printers'
PRINT '3. Right-click your printer'
PRINT '4. Select "Printer properties"'
PRINT '5. Copy the exact printer name'
PRINT ''
PRINT 'For network printers, use format:'
PRINT '\\ServerName\PrinterName'
PRINT ''
PRINT 'Common thermal printer names:'
PRINT '- Star TSP143'
PRINT '- Epson TM-T88'
PRINT '- Bixolon SRP-350'
PRINT '========================================='
