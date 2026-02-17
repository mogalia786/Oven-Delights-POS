import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Checking Bonaqua CategoryID and SubCategoryID...")
print()

cursor.execute("""
    SELECT SKU, Name, Category, CategoryID, SubCategoryID
    FROM Demo_Retail_Product
    WHERE Name LIKE '%Bonaqua%'
""")

print("Bonaqua products:")
for row in cursor.fetchall():
    cat_id = row[3] if row[3] else "NULL"
    subcat_id = row[4] if row[4] else "NULL"
    print(f"  {row[0]:20} Cat: {row[2]:15} CatID: {cat_id:5} SubCatID: {subcat_id}")

# Check what CategoryID 'drink' should be
print("\n\nChecking 'drink' category ID:")
cursor.execute("""
    SELECT CategoryID, CategoryName
    FROM Categories
    WHERE CategoryName LIKE '%drink%'
""")

drink_cats = cursor.fetchall()
if drink_cats:
    for row in drink_cats:
        print(f"  CategoryID {row[0]}: {row[1]}")
else:
    print("  No 'drink' category found in Categories table")

# Check all categories
print("\n\nAll categories:")
cursor.execute("SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName")
for row in cursor.fetchall():
    print(f"  {row[0]:3} - {row[1]}")

# Check products with NULL CategoryID
print("\n\nProducts with NULL CategoryID:")
cursor.execute("""
    SELECT COUNT(*)
    FROM Demo_Retail_Product
    WHERE IsActive = 1
    AND (CategoryID IS NULL OR SubCategoryID IS NULL)
""")

null_count = cursor.fetchone()[0]
print(f"  {null_count} products have NULL CategoryID or SubCategoryID")

conn.close()
