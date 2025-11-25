"""
Investigate:
1. Why name search doesn't show barcodes
2. Why Beverages category shows no products
3. Are prices correct
"""

import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("="*70)
print("INVESTIGATING POS ISSUES")
print("="*70)

# 1. Check Beverages category
print("\n1. BEVERAGES CATEGORY:")
cursor.execute("""
    SELECT CategoryID, CategoryName, IsActive
    FROM Categories
    WHERE CategoryName LIKE '%Beverage%'
""")

for row in cursor.fetchall():
    print(f"   CategoryID: {row[0]}, Name: {row[1]}, Active: {row[2]}")

# Check products in Beverages category
cursor.execute("""
    SELECT COUNT(*)
    FROM Demo_Retail_Product p
    WHERE p.CategoryID = 12  -- Beverages
    AND p.IsActive = 1
    AND p.ProductType IN ('External', 'Internal')
""")
bev_count = cursor.fetchone()[0]
print(f"   Products in Beverages (CategoryID=12): {bev_count}")

# Check subcategories for Beverages
print("\n   Subcategories in Beverages:")
cursor.execute("""
    SELECT DISTINCT sc.SubCategoryID, sc.SubCategoryName
    FROM SubCategories sc
    INNER JOIN Demo_Retail_Product p ON p.SubCategoryID = sc.SubCategoryID
    WHERE p.CategoryID = 12
    AND p.IsActive = 1
""")

subcats = cursor.fetchall()
if subcats:
    for row in subcats:
        print(f"      SubCategoryID: {row[0]}, Name: {row[1]}")
else:
    print("      ⚠ NO SUBCATEGORIES FOUND!")

# Sample products in Beverages
print("\n   Sample products in Beverages:")
cursor.execute("""
    SELECT TOP 5 p.SKU, p.Name, p.SubCategoryID, p.Barcode
    FROM Demo_Retail_Product p
    WHERE p.CategoryID = 12
    AND p.IsActive = 1
""")

for row in cursor.fetchall():
    barcode = row[3] if row[3] else "NO BARCODE"
    print(f"      {row[0]:20} {row[1]:40} SubCat: {row[2]:3} Barcode: {barcode}")

# 2. Check what view returns for Beverages products
print("\n\n2. WHAT VIEW RETURNS FOR BEVERAGES:")
cursor.execute("""
    SELECT COUNT(*)
    FROM vw_POS_Products
    WHERE Category = 'Beverages'
""")
view_count = cursor.fetchone()[0]
print(f"   vw_POS_Products returns {view_count} Beverages products")

# 3. Check barcode in view
print("\n\n3. BARCODE IN VIEW:")
cursor.execute("""
    SELECT TOP 5 ItemCode, ProductName, Barcode
    FROM vw_POS_Products
    WHERE ProductName LIKE '%Bonaqua%' OR ProductName LIKE '%Americano%'
""")

print("   Sample products from view:")
for row in cursor.fetchall():
    print(f"      {row[0]:20} {row[1]:40} Barcode: {row[2]}")

# 4. Check prices
print("\n\n4. PRICE CHECK:")
cursor.execute("""
    SELECT p.SKU, p.Name, pr.BranchID, pr.SellingPrice
    FROM Demo_Retail_Product p
    INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID
    WHERE p.Name LIKE '%Bonaqua%'
    ORDER BY p.SKU, pr.BranchID
""")

print("   Bonaqua prices by branch:")
for row in cursor.fetchall():
    branch = f"Branch {row[2]}" if row[2] else "Global"
    print(f"      {row[0]:20} {branch:15} R {row[3]:.2f}")

# 5. Check CategoryNavigationService query
print("\n\n5. CATEGORY NAVIGATION QUERY TEST:")
cursor.execute("""
    SELECT 
        sc.SubCategoryID,
        sc.SubCategoryName,
        COUNT(DISTINCT p.ProductID) AS ProductCount
    FROM SubCategories sc
    INNER JOIN Demo_Retail_Product p ON p.SubCategoryID = sc.SubCategoryID
    WHERE p.CategoryID = 12  -- Beverages
    AND p.IsActive = 1
    AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
    AND sc.IsActive = 1
    GROUP BY sc.SubCategoryID, sc.SubCategoryName
""")

print("   Subcategories that should show:")
results = cursor.fetchall()
if results:
    for row in results:
        print(f"      SubCat {row[0]:3}: {row[1]:30} ({row[2]} products)")
else:
    print("      ⚠ NO RESULTS - This is why category shows empty!")

# 6. Check if SubCategories table has IsActive column
print("\n\n6. SUBCATEGORIES TABLE STRUCTURE:")
cursor.execute("""
    SELECT COLUMN_NAME
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'SubCategories'
""")
print("   Columns:", [row[0] for row in cursor.fetchall()])

conn.close()

print("\n" + "="*70)
print("DIAGNOSIS COMPLETE")
print("="*70)
