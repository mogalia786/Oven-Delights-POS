-- Create TillPoints table to store till identification
IF OBJECT_ID('TillPoints', 'U') IS NOT NULL
    DROP TABLE TillPoints
GO

CREATE TABLE TillPoints (
    TillPointID INT IDENTITY(1,1) PRIMARY KEY,
    TillNumber NVARCHAR(50) NOT NULL UNIQUE,
    BranchID INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy INT NULL,
    LastModifiedDate DATETIME NULL,
    LastModifiedBy INT NULL,
    CONSTRAINT FK_TillPoints_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
)
GO

-- Add TillPointID to Sales table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TillPointID')
BEGIN
    ALTER TABLE Sales
    ADD TillPointID INT NULL
    
    PRINT 'Added TillPointID column to Sales table'
END
GO

-- Add foreign key constraint
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Sales_TillPoint')
BEGIN
    ALTER TABLE Sales
    ADD CONSTRAINT FK_Sales_TillPoint FOREIGN KEY (TillPointID) REFERENCES TillPoints(TillPointID)
    
    PRINT 'Added foreign key constraint FK_Sales_TillPoint'
END
GO

PRINT 'TillPoints table created successfully!'
