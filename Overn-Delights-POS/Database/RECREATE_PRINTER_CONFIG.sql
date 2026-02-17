-- Drop and recreate PrinterConfig table with correct structure

-- Drop existing table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PrinterConfig')
BEGIN
    DROP TABLE PrinterConfig
    PRINT 'Dropped existing PrinterConfig table'
END

-- Create new table with correct structure
CREATE TABLE PrinterConfig (
    BranchID INT NOT NULL PRIMARY KEY,
    PrinterName NVARCHAR(255) NOT NULL,
    PrinterIPAddress NVARCHAR(50) NULL,
    IsNetworkPrinter BIT NOT NULL DEFAULT 0,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_PrinterConfig_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
)

PRINT 'Created PrinterConfig table'

-- Insert default printer for each branch (using Epson slip printer)
INSERT INTO PrinterConfig (BranchID, PrinterName, IsNetworkPrinter)
SELECT 
    BranchID,
    'EPSON TM-T20' AS PrinterName,  -- Change this to your actual Epson printer name
    0 AS IsNetworkPrinter
FROM Branches

PRINT 'Added default Epson slip printer for all branches'

-- Show configuration
SELECT 
    b.BranchName,
    pc.PrinterName,
    pc.PrinterIPAddress,
    CASE WHEN pc.IsNetworkPrinter = 1 THEN 'Network' ELSE 'Local Slip Printer' END AS PrinterType
FROM PrinterConfig pc
INNER JOIN Branches b ON b.BranchID = pc.BranchID
ORDER BY b.BranchName

PRINT ''
PRINT 'IMPORTANT: Update the PrinterName to match your actual Epson printer name!'
PRINT 'Run this to update: UPDATE PrinterConfig SET PrinterName = ''YourActualPrinterName'''

GO
