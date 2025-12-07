-- =============================================
-- Till Day End Control System
-- Prevents fraud by ensuring all tills complete day-end before next day login
-- =============================================

-- Drop existing table if exists
IF OBJECT_ID('TillDayEnd', 'U') IS NOT NULL
    DROP TABLE TillDayEnd
GO

-- Create TillDayEnd table
CREATE TABLE TillDayEnd (
    DayEndID INT IDENTITY(1,1) PRIMARY KEY,
    TillPointID INT NOT NULL,
    BusinessDate DATE NOT NULL,
    IsDayEnd BIT DEFAULT 0,
    DayEndTime DATETIME NULL,
    CashierID INT NOT NULL,
    CashierName NVARCHAR(100),
    
    -- Cash-up totals
    TotalSales DECIMAL(18,2) DEFAULT 0,
    TotalCash DECIMAL(18,2) DEFAULT 0,
    TotalCard DECIMAL(18,2) DEFAULT 0,
    TotalAccount DECIMAL(18,2) DEFAULT 0,
    TotalRefunds DECIMAL(18,2) DEFAULT 0,
    
    -- Cash drawer counts
    ExpectedCash DECIMAL(18,2) DEFAULT 0,
    ActualCash DECIMAL(18,2) DEFAULT 0,
    CashVariance DECIMAL(18,2) DEFAULT 0,
    
    -- Audit fields
    PrintedReceipt BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CompletedBy INT NULL,
    Notes NVARCHAR(500),
    
    -- Constraints
    CONSTRAINT FK_TillDayEnd_TillPoint FOREIGN KEY (TillPointID) REFERENCES TillPoints(TillPointID),
    CONSTRAINT UQ_TillDayEnd UNIQUE (TillPointID, BusinessDate)
)
GO

-- Create index for fast previous day lookup
CREATE INDEX IX_TillDayEnd_BusinessDate_IsDayEnd 
ON TillDayEnd(BusinessDate, IsDayEnd)
GO

-- Create index for till point lookup
CREATE INDEX IX_TillDayEnd_TillPoint 
ON TillDayEnd(TillPointID, BusinessDate)
GO

PRINT 'TillDayEnd table created successfully!'
PRINT 'Day End Control System is now active.'
GO

-- Sample query to check incomplete day-ends
PRINT ''
PRINT 'To check incomplete day-ends for yesterday:'
PRINT 'SELECT * FROM TillDayEnd WHERE BusinessDate = CAST(DATEADD(DAY, -1, GETDATE()) AS DATE) AND IsDayEnd = 0'
GO
