-- Force fix PrinterConfig table

-- Drop foreign key constraint if exists
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PrinterConfig_Branch')
BEGIN
    ALTER TABLE PrinterConfig DROP CONSTRAINT FK_PrinterConfig_Branch
    PRINT 'Dropped FK constraint'
END

-- Drop table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PrinterConfig')
BEGIN
    DROP TABLE PrinterConfig
    PRINT 'Dropped PrinterConfig table'
END
ELSE
BEGIN
    PRINT 'PrinterConfig table does not exist'
END

-- Create simple table - just BranchID and PrinterName
CREATE TABLE PrinterConfig (
    BranchID INT NOT NULL PRIMARY KEY,
    PrinterName NVARCHAR(255) NOT NULL,
    PrinterIPAddress NVARCHAR(50) NULL,
    IsNetworkPrinter BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_PrinterConfig_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
)

PRINT 'Created new PrinterConfig table'

-- Add default printer for each branch
INSERT INTO PrinterConfig (BranchID, PrinterName, IsNetworkPrinter)
SELECT 
    BranchID,
    'Microsoft Print to PDF',  -- Temporary - you'll update this
    0
FROM Branches

PRINT 'Added default printers'
PRINT ''
PRINT 'Now update with your actual printer name:'
PRINT 'UPDATE PrinterConfig SET PrinterName = ''YourEpsonPrinterName'''

-- Show what we have
SELECT * FROM PrinterConfig

GO
