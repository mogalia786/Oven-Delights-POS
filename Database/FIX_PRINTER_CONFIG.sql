-- Fix PrinterConfig table - add missing columns if they don't exist

-- Add IsNetworkPrinter column if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PrinterConfig' AND COLUMN_NAME = 'IsNetworkPrinter')
BEGIN
    ALTER TABLE PrinterConfig ADD IsNetworkPrinter BIT NOT NULL DEFAULT 0
    PRINT 'Added IsNetworkPrinter column'
END

-- Add IsActive column if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PrinterConfig' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE PrinterConfig ADD IsActive BIT NOT NULL DEFAULT 1
    PRINT 'Added IsActive column'
END

-- Add PrinterIPAddress column if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PrinterConfig' AND COLUMN_NAME = 'PrinterIPAddress')
BEGIN
    ALTER TABLE PrinterConfig ADD PrinterIPAddress NVARCHAR(50) NULL
    PRINT 'Added PrinterIPAddress column'
END

-- Add CreatedDate column if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PrinterConfig' AND COLUMN_NAME = 'CreatedDate')
BEGIN
    ALTER TABLE PrinterConfig ADD CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
    PRINT 'Added CreatedDate column'
END

-- Insert default printer for branches that don't have one
INSERT INTO PrinterConfig (BranchID, PrinterName, IsNetworkPrinter, IsActive)
SELECT 
    b.BranchID,
    'Microsoft Print to PDF' AS PrinterName,
    0 AS IsNetworkPrinter,
    1 AS IsActive
FROM Branches b
WHERE NOT EXISTS (
    SELECT 1 FROM PrinterConfig pc 
    WHERE pc.BranchID = b.BranchID 
    AND pc.IsNetworkPrinter = 0
)

PRINT 'Printer configuration updated successfully'

-- Show current configuration
SELECT 
    pc.*,
    b.BranchName
FROM PrinterConfig pc
INNER JOIN Branches b ON b.BranchID = pc.BranchID
ORDER BY b.BranchName

GO
