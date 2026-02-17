import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Checking for products without prices...")

# Find products that don't have any price records
cursor.execute("""
    SELECT p.ProductID, p.SKU, p.Name, p.Category
    FROM Demo_Retail_Product p
    WHERE p.IsActive = 1
    AND p.ProductType IN ('External', 'Internal')
    AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    AND NOT EXISTS (
        SELECT 1 FROM Demo_Retail_Price pr 
        WHERE pr.ProductID = p.ProductID
    )
    ORDER BY p.Name
""")

products_without_prices = cursor.fetchall()

print(f"\n✗ Found {len(products_without_prices)} products WITHOUT any prices")

if len(products_without_prices) > 0:
    print("\nSample products without prices:")
    for row in products_without_prices[:20]:
        print(f"  {row[1]:20} {row[2]:50} Cat: {row[3]}")
    
    if len(products_without_prices) > 20:
        print(f"  ... and {len(products_without_prices) - 20} more")

# Check Bonaqua specifically
cursor.execute("""
    SELECT p.ProductID, p.SKU, p.Name, 
           (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
            WHERE ProductID = p.ProductID 
            ORDER BY EffectiveFrom DESC) AS Price
    FROM Demo_Retail_Product p
    WHERE p.Name LIKE '%Bonaqua%' AND p.Category = 'drink'
""")

print("\n\nBonaqua products pricing:")
for row in cursor.fetchall():
    price = row[3] if row[3] else "NO PRICE"
    print(f"  {row[1]:20} {row[2]:40} Price: {price}")

conn.close()

print("\n" + "="*70)
if len(products_without_prices) > 0:
    print(f"⚠ WARNING: {len(products_without_prices)} products need prices added!")
    print("These products will show R 0.00 in POS until prices are added.")
else:
    print("✓ All products have prices!")
