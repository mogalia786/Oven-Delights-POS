-- Check if PrinterConfig and ReceiptTemplateConfig tables exist
-- Run this on Azure database: mogalia.database.windows.net -> Oven_Delights_Main

SELECT 'PrinterConfig' AS TableName, COUNT(*) AS Exists
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'PrinterConfig'

UNION ALL

SELECT 'ReceiptTemplateConfig' AS TableName, COUNT(*) AS Exists
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'ReceiptTemplateConfig'

-- If tables exist, check data
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PrinterConfig')
BEGIN
    SELECT 'PrinterConfig Data:' AS Info
    SELECT * FROM PrinterConfig
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ReceiptTemplateConfig')
BEGIN
    SELECT 'ReceiptTemplateConfig Data:' AS Info
    SELECT * FROM ReceiptTemplateConfig
END
