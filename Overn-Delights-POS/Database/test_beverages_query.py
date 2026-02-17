import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Testing GetSubCategories query for Beverages (CategoryID=12):")
print()

# This is the exact query from CategoryNavigationService
cursor.execute("""
    SELECT DISTINCT
        sc.SubCategoryID,
        sc.CategoryID,
        sc.SubCategoryName,
        sc.DisplayOrder,
        COUNT(DISTINCT p.ProductID) AS ProductCount
    FROM SubCategories sc
    INNER JOIN Demo_Retail_Product p ON p.SubCategoryID = sc.SubCategoryID 
        AND p.IsActive = 1
        AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
    WHERE sc.CategoryID = 12
      AND sc.IsActive = 1
    GROUP BY sc.SubCategoryID, sc.CategoryID, sc.SubCategoryName, sc.DisplayOrder
    HAVING COUNT(DISTINCT p.ProductID) > 0
    ORDER BY sc.DisplayOrder, sc.SubCategoryName
""")

results = cursor.fetchall()

if results:
    print(f"✓ Query returns {len(results)} subcategories:")
    for row in results:
        print(f"  SubCategoryID: {row[0]}")
        print(f"  CategoryID: {row[1]}")
        print(f"  SubCategoryName: {row[2]}")
        print(f"  DisplayOrder: {row[3]}")
        print(f"  ProductCount: {row[4]}")
        print()
else:
    print("✗ Query returns NO results!")
    print()
    print("Debugging:")
    
    # Check if subcategory exists
    cursor.execute("SELECT SubCategoryID, SubCategoryName, IsActive FROM SubCategories WHERE CategoryID = 12")
    print("\nSubcategories for CategoryID 12:")
    for row in cursor.fetchall():
        print(f"  {row[0]}: {row[1]} (Active: {row[2]})")
    
    # Check if products exist
    cursor.execute("""
        SELECT COUNT(*) 
        FROM Demo_Retail_Product 
        WHERE CategoryID = 12 
        AND SubCategoryID = 26 
        AND IsActive = 1 
        AND ProductType IN ('External', 'Internal')
    """)
    count = cursor.fetchone()[0]
    print(f"\nProducts matching criteria: {count}")

conn.close()
