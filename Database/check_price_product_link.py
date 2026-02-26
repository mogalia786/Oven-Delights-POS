import pyodbc

conn_str = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=tcp:mogalia.database.windows.net,1433;"
    "Database=Oven_Delights_Main;"
    "Uid=faroq786;"
    "Pwd=Faroq#786;"
    "Encrypt=yes;"
    "TrustServerCertificate=no;"
    "Connection Timeout=30;"
)

conn = pyodbc.connect(conn_str)
cursor = conn.cursor()

# Check a specific SKU to understand the relationship
print("Checking SKU 'SRN-STA-3TI' (3 Tier Stack Sponge):")
print("="*80)

# Find all products with this SKU
cursor.execute("""
    SELECT ProductID, SKU, Name, BranchID 
    FROM Demo_Retail_Product 
    WHERE SKU = 'SRN-STA-3TI'
    ORDER BY BranchID
""")
print("\nProducts with SKU 'SRN-STA-3TI':")
print("ProductID | SKU | Name | BranchID")
for row in cursor.fetchall():
    print(f"{row[0]} | {row[1]} | {row[2][:40]} | {row[3]}")

# Find all prices for this SKU
cursor.execute("""
    SELECT pr.PriceID, pr.ProductID, pr.BranchID, pr.SellingPrice, p.SKU, p.BranchID AS ProductBranchID
    FROM Demo_Retail_Price pr
    LEFT JOIN Demo_Retail_Product p ON pr.ProductID = p.ProductID
    WHERE pr.ProductID IN (SELECT ProductID FROM Demo_Retail_Product WHERE SKU = 'SRN-STA-3TI')
    ORDER BY pr.BranchID
""")
print("\nPrices for products with SKU 'SRN-STA-3TI':")
print("PriceID | ProductID | PriceBranchID | SellingPrice | ProductSKU | ProductBranchID")
for row in cursor.fetchall():
    print(f"{row[0]} | {row[1]} | {row[2]} | {row[3]} | {row[4]} | {row[5]}")

# Check overall stats
cursor.execute("""
    SELECT 
        COUNT(DISTINCT p.ProductID) AS TotalProducts,
        COUNT(DISTINCT CASE WHEN pr.PriceID IS NOT NULL THEN p.ProductID END) AS ProductsWithPrices
    FROM Demo_Retail_Product p
    LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = p.BranchID
    WHERE p.BranchID = 4 AND p.IsActive = 1
""")
print("\n" + "="*80)
print("Umhlanga (BranchID=4) Statistics:")
result = cursor.fetchone()
print(f"Total Products: {result[0]}")
print(f"Products with matching prices: {result[1]}")
print(f"Products WITHOUT prices: {result[0] - result[1]}")

cursor.close()
conn.close()
