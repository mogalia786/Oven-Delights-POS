import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("="*70)
print("BONAQUA DEBUG - Why no price/barcode in POS?")
print("="*70)

# Check raw product data
print("\n1. RAW PRODUCT DATA:")
cursor.execute("""
    SELECT ProductID, SKU, Name, Category, ProductType, IsActive, BranchID, Barcode, CurrentStock
    FROM Demo_Retail_Product
    WHERE Name LIKE '%Bonaqua%'
    ORDER BY Name
""")

for row in cursor.fetchall():
    print(f"\nProductID: {row[0]}")
    print(f"  SKU: {row[1]}")
    print(f"  Name: {row[2]}")
    print(f"  Category: {row[3]}")
    print(f"  ProductType: {row[4]}")
    print(f"  IsActive: {row[5]}")
    print(f"  BranchID: {row[6]}")
    print(f"  Barcode: {row[7]}")
    print(f"  CurrentStock: {row[8]}")

# Check prices for Branch 6 (Ayesha Centre - from your screenshot)
print("\n\n2. PRICES FOR BRANCH 6 (AYESHA CENTRE):")
cursor.execute("""
    SELECT p.SKU, p.Name, pr.BranchID, pr.SellingPrice, pr.CostPrice
    FROM Demo_Retail_Product p
    LEFT JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 6
    WHERE p.Name LIKE '%Bonaqua%'
""")

for row in cursor.fetchall():
    branch = row[2] if row[2] else "NO PRICE FOR BRANCH 6"
    price = f"R {row[3]:.2f}" if row[3] else "NULL"
    print(f"  {row[0]:20} {row[1]:40} Branch: {branch} Price: {price}")

# Check what the view returns
print("\n\n3. WHAT VW_POS_PRODUCTS VIEW RETURNS:")
cursor.execute("""
    SELECT * FROM vw_POS_Products
    WHERE ProductName LIKE '%Bonaqua%'
""")

columns = [column[0] for column in cursor.description]
print(f"Columns: {columns}")

for row in cursor.fetchall():
    print(f"\n{row[1]} - {row[2]}")  # SKU - Name
    for i, col in enumerate(columns):
        print(f"  {col}: {row[i]}")

# Check category navigation query
print("\n\n4. CATEGORY NAVIGATION QUERY (drink category):")
cursor.execute("""
    SELECT 
        p.ProductID,
        p.SKU AS ItemCode,
        p.Name AS ProductName,
        p.Code AS ProductCode,
        ISNULL(p.Barcode, p.SKU) AS Barcode,
        ISNULL(p.CurrentStock, 0) AS QtyOnHand
    FROM Demo_Retail_Product p
    WHERE p.IsActive = 1
    AND p.Category = 'drink'
    AND p.Name LIKE '%Bonaqua%'
""")

print("Results:")
for row in cursor.fetchall():
    print(f"  {row[1]:20} {row[2]:40} Barcode: {row[4]} Stock: {row[5]}")

conn.close()
