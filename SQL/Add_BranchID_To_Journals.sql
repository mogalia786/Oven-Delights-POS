-- Add BranchID column to Journals table if it doesn't exist

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Journals') AND name = 'BranchID')
BEGIN
    ALTER TABLE Journals ADD BranchID INT NULL
    PRINT 'Added BranchID column to Journals table'
    
    -- Update existing records with a default BranchID (first active branch)
    DECLARE @DefaultBranchID INT
    SELECT TOP 1 @DefaultBranchID = BranchID FROM Branches WHERE IsActive = 1 ORDER BY BranchID
    
    IF @DefaultBranchID IS NOT NULL
    BEGIN
        UPDATE Journals SET BranchID = @DefaultBranchID WHERE BranchID IS NULL
        PRINT 'Updated existing Journals records with BranchID: ' + CAST(@DefaultBranchID AS VARCHAR)
    END
    
    -- Make BranchID NOT NULL after updating existing records
    ALTER TABLE Journals ALTER COLUMN BranchID INT NOT NULL
    PRINT 'Made BranchID column NOT NULL'
END
ELSE
BEGIN
    PRINT 'BranchID column already exists in Journals table'
END
GO
