-- =============================================
-- CAKE ORDER SYSTEM - SAMPLE DATA
-- Insert questions, options, and prices for testing
-- =============================================

DECLARE @BranchID INT = 1 -- Change to your branch ID

-- Clear existing data (for testing)
DELETE FROM CakeOrder_QuestionOptions
DELETE FROM CakeOrder_Questions

PRINT 'Inserting Cake Order Questions and Options...'
PRINT ''

-- QUESTION 1: Shape of Cake
DECLARE @Q1 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'Shape of Cake?', 'SingleChoice', 1)
SET @Q1 = SCOPE_IDENTITY()

INSERT INTO CakeOrder_QuestionOptions (QuestionID, OptionText, Price, DisplayOrder) VALUES
(@Q1, 'Round', 0.00, 1),
(@Q1, 'Square', 50.00, 2),
(@Q1, 'Heart', 100.00, 3),
(@Q1, 'Rectangle', 75.00, 4),
(@Q1, 'Custom Shape', 200.00, 5)

PRINT '✓ Question 1: Shape of Cake (5 options)'

-- QUESTION 2: Size of Cake
DECLARE @Q2 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'Size of Cake (diameter)?', 'SingleChoice', 2)
SET @Q2 = SCOPE_IDENTITY()

INSERT INTO CakeOrder_QuestionOptions (QuestionID, OptionText, Price, DisplayOrder) VALUES
(@Q2, '10cm', 150.00, 1),
(@Q2, '20cm', 300.00, 2),
(@Q2, '30cm', 500.00, 3),
(@Q2, '40cm', 750.00, 4),
(@Q2, '50cm', 1000.00, 5),
(@Q2, '60cm', 1500.00, 6)

PRINT '✓ Question 2: Size of Cake (6 options)'

-- QUESTION 3: Number of Layers
DECLARE @Q3 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'How many sponge layers?', 'SingleChoice', 3)
SET @Q3 = SCOPE_IDENTITY()

INSERT INTO CakeOrder_QuestionOptions (QuestionID, OptionText, Price, DisplayOrder) VALUES
(@Q3, '1 Layer', 0.00, 1),
(@Q3, '2 Layers', 80.00, 2),
(@Q3, '3 Layers', 150.00, 3),
(@Q3, '4 Layers', 220.00, 4),
(@Q3, '5 Layers', 300.00, 5)

PRINT '✓ Question 3: Number of Layers (5 options)'

-- QUESTION 4: Flavour
DECLARE @Q4 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'Cake Flavour?', 'SingleChoice', 4)
SET @Q4 = SCOPE_IDENTITY()

INSERT INTO CakeOrder_QuestionOptions (QuestionID, OptionText, Price, DisplayOrder) VALUES
(@Q4, 'Vanilla', 0.00, 1),
(@Q4, 'Chocolate', 50.00, 2),
(@Q4, 'Red Velvet', 100.00, 3),
(@Q4, 'Carrot Cake', 80.00, 4),
(@Q4, 'Lemon', 60.00, 5),
(@Q4, 'Strawberry', 70.00, 6),
(@Q4, 'Coffee', 75.00, 7),
(@Q4, 'Black Forest', 120.00, 8)

PRINT '✓ Question 4: Flavour (8 options)'

-- QUESTION 5: Picture on Cake
DECLARE @Q5 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'Picture on cake?', 'SingleChoice', 5)
SET @Q5 = SCOPE_IDENTITY()

INSERT INTO CakeOrder_QuestionOptions (QuestionID, OptionText, Price, DisplayOrder) VALUES
(@Q5, 'No Picture', 0.00, 1),
(@Q5, 'Edible Print (Small)', 150.00, 2),
(@Q5, 'Edible Print (Large)', 300.00, 3),
(@Q5, 'Hand Painted', 500.00, 4)

PRINT '✓ Question 5: Picture on Cake (4 options)'

-- QUESTION 6: Accessories
DECLARE @Q6 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'Accessories (Select from list)', 'MultiChoice', 6)
SET @Q6 = SCOPE_IDENTITY()

PRINT '✓ Question 6: Accessories (will use Accessories table)'

-- QUESTION 7: Toppings
DECLARE @Q7 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'Toppings (Select from list)', 'MultiChoice', 7)
SET @Q7 = SCOPE_IDENTITY()

PRINT '✓ Question 7: Toppings (will use Toppings table)'

-- QUESTION 8: Wording on Cake
DECLARE @Q8 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'Wording on cake (Name/Message)?', 'Text', 8)
SET @Q8 = SCOPE_IDENTITY()

INSERT INTO CakeOrder_QuestionOptions (QuestionID, OptionText, Price, DisplayOrder) VALUES
(@Q8, 'No Wording', 0.00, 1),
(@Q8, 'Custom Wording (up to 20 characters)', 50.00, 2),
(@Q8, 'Custom Wording (21-50 characters)', 100.00, 3)

PRINT '✓ Question 8: Wording on Cake (3 options)'

-- QUESTION 9: Cream Type
DECLARE @Q9 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'Cream Type?', 'SingleChoice', 9)
SET @Q9 = SCOPE_IDENTITY()

INSERT INTO CakeOrder_QuestionOptions (QuestionID, OptionText, Price, DisplayOrder) VALUES
(@Q9, 'Fresh Cream', 0.00, 1),
(@Q9, 'Butter Cream', 80.00, 2)

PRINT '✓ Question 9: Cream Type (2 options)'

-- QUESTION 10: Finish
DECLARE @Q10 INT
INSERT INTO CakeOrder_Questions (BranchID, QuestionText, QuestionType, DisplayOrder)
VALUES (@BranchID, 'Cake Finish?', 'SingleChoice', 10)
SET @Q10 = SCOPE_IDENTITY()

INSERT INTO CakeOrder_QuestionOptions (QuestionID, OptionText, Price, DisplayOrder) VALUES
(@Q10, 'Icing', 0.00, 1),
(@Q10, 'Fondant', 200.00, 2)

PRINT '✓ Question 10: Cake Finish (2 options)'

PRINT ''
PRINT '========================================='
PRINT 'Sample Data Inserted Successfully!'
PRINT 'Total Questions: 10'
PRINT '========================================='

-- Display summary
SELECT 
    q.QuestionID,
    q.QuestionText,
    q.QuestionType,
    COUNT(o.OptionID) AS OptionCount
FROM CakeOrder_Questions q
LEFT JOIN CakeOrder_QuestionOptions o ON q.QuestionID = o.QuestionID
WHERE q.BranchID = @BranchID
GROUP BY q.QuestionID, q.QuestionText, q.QuestionType, q.DisplayOrder
ORDER BY q.DisplayOrder
