-- Create Retail Supervisor role for Till Point management
IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'Retail Supervisor')
BEGIN
    INSERT INTO Roles (RoleName, Description, IsActive, CreatedDate)
    VALUES ('Retail Supervisor', 'Retail Supervisor - Can setup Till Points and manage POS operations', 1, GETDATE())
    
    PRINT 'Created Retail Supervisor role'
END
ELSE
BEGIN
    PRINT 'Retail Supervisor role already exists'
END
GO
