import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Checking category table structure...")

# Check if category tables exist
cursor.execute("""
    SELECT TABLE_NAME 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_NAME LIKE '%Category%' OR TABLE_NAME LIKE '%SubCategory%'
    ORDER BY TABLE_NAME
""")

print("\nCategory-related tables:")
for row in cursor.fetchall():
    print(f"  {row[0]}")

# Check Bonaqua in category tables
print("\n\nChecking Bonaqua in Demo_Retail_ProductCategory:")
cursor.execute("""
    SELECT pc.*, p.Name
    FROM Demo_Retail_ProductCategory pc
    INNER JOIN Demo_Retail_Product p ON p.ProductID = pc.ProductID
    WHERE p.Name LIKE '%Bonaqua%'
""")

results = cursor.fetchall()
if results:
    for row in results:
        print(f"  Found: {row}")
else:
    print("  ⚠ NO ENTRIES - Bonaqua not in ProductCategory table!")

# Check structure of ProductCategory table
print("\n\nDemo_Retail_ProductCategory table structure:")
cursor.execute("""
    SELECT COLUMN_NAME, DATA_TYPE 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Demo_Retail_ProductCategory'
    ORDER BY ORDINAL_POSITION
""")

for row in cursor.fetchall():
    print(f"  {row[0]:30} {row[1]}")

# Check how many products are missing from category table
print("\n\nProducts missing from Demo_Retail_ProductCategory:")
cursor.execute("""
    SELECT COUNT(*)
    FROM Demo_Retail_Product p
    WHERE p.IsActive = 1
    AND p.ProductType IN ('External', 'Internal')
    AND NOT EXISTS (
        SELECT 1 FROM Demo_Retail_ProductCategory pc
        WHERE pc.ProductID = p.ProductID
    )
""")

missing_count = cursor.fetchone()[0]
print(f"  {missing_count} products missing from category table")

# Sample missing products
if missing_count > 0:
    cursor.execute("""
        SELECT TOP 10 p.SKU, p.Name, p.Category
        FROM Demo_Retail_Product p
        WHERE p.IsActive = 1
        AND p.ProductType IN ('External', 'Internal')
        AND NOT EXISTS (
            SELECT 1 FROM Demo_Retail_ProductCategory pc
            WHERE pc.ProductID = p.ProductID
        )
    """)
    
    print("\n  Sample missing products:")
    for row in cursor.fetchall():
        print(f"    {row[0]:20} {row[1]:40} Cat: {row[2]}")

conn.close()
