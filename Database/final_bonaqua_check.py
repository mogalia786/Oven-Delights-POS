import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("="*70)
print("FINAL BONAQUA CHECK")
print("="*70)

# 1. Check if Bonaqua has prices for Branch 6
print("\n1. Bonaqua prices in Branch 6:")
cursor.execute("""
    SELECT p.SKU, p.Name, pr.SellingPrice, p.Barcode
    FROM Demo_Retail_Product p
    LEFT JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 6
    WHERE p.Name LIKE '%Bonaqua%'
""")

has_prices = False
for row in cursor.fetchall():
    price = f"R {row[2]:.2f}" if row[2] else "NO PRICE"
    barcode = row[3] if row[3] else "NO BARCODE"
    print(f"  {row[0]:20} {row[1]:40} {price:10} {barcode}")
    if row[2]:
        has_prices = True

if not has_prices:
    print("\n  ⚠ NO PRICES FOR BRANCH 6 - SQL SCRIPT NOT RUN YET!")
    print("  Run ADD_ALL_PRODUCTS_TO_ALL_BRANCHES.sql first!")

# 2. Check what category navigation would return
print("\n2. Category navigation query for 'drink' category:")
cursor.execute("""
    SELECT 
        p.ProductID,
        p.SKU AS ItemCode,
        p.Name AS ProductName,
        ISNULL(p.Barcode, p.SKU) AS Barcode,
        p.Category,
        p.BranchID
    FROM Demo_Retail_Product p
    WHERE p.IsActive = 1
    AND p.Category = 'drink'
    AND p.Name LIKE '%Bonaqua%'
""")

results = cursor.fetchall()
print(f"  Found {len(results)} Bonaqua products in 'drink' category:")
for row in results:
    print(f"    {row[1]:20} {row[2]:40} Branch: {row[5]}")

# 3. Check if products are in correct branch
print("\n3. Which branch is POS running on?")
print("   If POS is on Branch 6, products must have BranchID = 6 OR be in all branches")
print("   Current Bonaqua BranchID: 6")

# 4. Suggest fix
print("\n" + "="*70)
print("SOLUTION:")
print("="*70)
if not has_prices:
    print("1. Run ADD_ALL_PRODUCTS_TO_ALL_BRANCHES.sql in SSMS")
    print("2. This will add prices for all products to all branches")
    print("3. Rebuild POS application")
    print("4. Bonaqua will appear with price and barcode")
else:
    print("✓ Prices exist - rebuild POS to see changes")

conn.close()
