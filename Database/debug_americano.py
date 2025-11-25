import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("="*70)
print("DEBUGGING AMERICANO TALL")
print("="*70)

# Check Americano in database
cursor.execute("""
    SELECT 
        p.ProductID,
        p.SKU,
        p.Name,
        p.Category,
        p.CategoryID,
        p.SubCategoryID,
        p.ProductType,
        p.IsActive,
        p.BranchID,
        c.CategoryName,
        sc.SubCategoryName
    FROM Demo_Retail_Product p
    LEFT JOIN Categories c ON c.CategoryID = p.CategoryID
    LEFT JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
    WHERE p.Name LIKE '%Americano Tall%'
""")

print("\nAmericano Tall in database:")
row = cursor.fetchone()
if row:
    print(f"  ProductID: {row[0]}")
    print(f"  SKU: {row[1]}")
    print(f"  Name: {row[2]}")
    print(f"  Category (text): {row[3]}")
    print(f"  CategoryID: {row[4]}")
    print(f"  SubCategoryID: {row[5]}")
    print(f"  ProductType: {row[6]}")
    print(f"  IsActive: {row[7]}")
    print(f"  BranchID: {row[8]}")
    print(f"  CategoryName: {row[9]}")
    print(f"  SubCategoryName: {row[10]}")
else:
    print("  NOT FOUND!")

# Check what category ID Beverages is
print("\n\nBeverages category:")
cursor.execute("SELECT CategoryID, CategoryName FROM Categories WHERE CategoryName LIKE '%Beverage%'")
for row in cursor.fetchall():
    print(f"  CategoryID {row[0]}: {row[1]}")

# Check the subcategory query
print("\n\nSubcategory query for Beverages (CategoryID=12):")
cursor.execute("""
    SELECT DISTINCT
        sc.SubCategoryID,
        sc.CategoryID,
        sc.SubCategoryName,
        sc.DisplayOrder,
        COUNT(DISTINCT p.ProductID) AS ProductCount
    FROM SubCategories sc
    LEFT JOIN Demo_Retail_Product p ON p.SubCategoryID = sc.SubCategoryID 
        AND p.IsActive = 1
        AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
        AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
    WHERE sc.CategoryID = 12
      AND sc.IsActive = 1
    GROUP BY sc.SubCategoryID, sc.CategoryID, sc.SubCategoryName, sc.DisplayOrder
    HAVING COUNT(DISTINCT p.ProductID) > 0
    ORDER BY sc.DisplayOrder, sc.SubCategoryName
""")

results = cursor.fetchall()
if results:
    for row in results:
        print(f"  SubCat {row[0]}: {row[2]} ({row[4]} products)")
else:
    print("  NO SUBCATEGORIES RETURNED!")

# Check products that should be in Beverages
print("\n\nProducts with CategoryID=12 and SubCategoryID=26:")
cursor.execute("""
    SELECT COUNT(*)
    FROM Demo_Retail_Product
    WHERE CategoryID = 12
    AND SubCategoryID = 26
    AND IsActive = 1
    AND ProductType IN ('External', 'Internal')
    AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
""")
count = cursor.fetchone()[0]
print(f"  {count} products match all criteria")

# Check if Americano is in that count
cursor.execute("""
    SELECT SKU, Name
    FROM Demo_Retail_Product
    WHERE CategoryID = 12
    AND SubCategoryID = 26
    AND IsActive = 1
    AND ProductType IN ('External', 'Internal')
    AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    AND Name LIKE '%Americano%'
""")

print("\n  Americano products in that count:")
for row in cursor.fetchall():
    print(f"    {row[0]:20} {row[1]}")

conn.close()
