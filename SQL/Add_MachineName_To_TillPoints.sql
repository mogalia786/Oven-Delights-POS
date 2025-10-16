-- Add MachineName column to TillPoints table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TillPoints') AND name = 'MachineName')
BEGIN
    ALTER TABLE TillPoints
    ADD MachineName NVARCHAR(100) NULL
    
    PRINT 'Added MachineName column to TillPoints table'
END
ELSE
BEGIN
    PRINT 'MachineName column already exists in TillPoints table'
END
GO

-- Make MachineName NOT NULL after adding it
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TillPoints') AND name = 'MachineName' AND is_nullable = 1)
BEGIN
    -- First update any NULL values
    UPDATE TillPoints SET MachineName = 'UNKNOWN' WHERE MachineName IS NULL
    
    -- Then make it NOT NULL
    ALTER TABLE TillPoints
    ALTER COLUMN MachineName NVARCHAR(100) NOT NULL
    
    PRINT 'MachineName column set to NOT NULL'
END
GO

PRINT 'TillPoints table updated successfully!'
