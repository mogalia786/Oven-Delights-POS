import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Fixing Bonaqua prices for Branch 6...")

# Get Bonaqua product IDs
cursor.execute("""
    SELECT ProductID, SKU, Name
    FROM Demo_Retail_Product
    WHERE Name LIKE '%Bonaqua%' AND BranchID = 6
""")

bonaqua_products = cursor.fetchall()

print(f"Found {len(bonaqua_products)} Bonaqua products in Branch 6")

# Add prices for each product
for product in bonaqua_products:
    product_id = product[0]
    sku = product[1]
    name = product[2]
    
    # Check if price exists
    cursor.execute("""
        SELECT COUNT(*) FROM Demo_Retail_Price
        WHERE ProductID = ? AND BranchID = 6
    """, product_id)
    
    exists = cursor.fetchone()[0] > 0
    
    if not exists:
        # Add price (R 15.00 for drinks)
        cursor.execute("""
            INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
            VALUES (?, 6, 15.00, 8.00, GETDATE())
        """, product_id)
        
        print(f"  ✓ Added price R 15.00 for {sku} - {name}")
    else:
        print(f"  - Price already exists for {sku}")

conn.commit()

# Verify
print("\nVerifying prices:")
cursor.execute("""
    SELECT p.SKU, p.Name, pr.SellingPrice
    FROM Demo_Retail_Product p
    INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 6
    WHERE p.Name LIKE '%Bonaqua%'
""")

for row in cursor.fetchall():
    print(f"  {row[0]:20} {row[1]:40} R {row[2]:.2f}")

conn.close()

print("\n✓ Bonaqua prices fixed for Branch 6!")
print("Rebuild POS to see the changes.")
