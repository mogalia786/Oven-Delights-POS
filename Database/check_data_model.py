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

print("Checking proper data model structure:")
print("="*80)

# Check if products should have BranchID or not
cursor.execute("""
    SELECT TOP 5
        p.ProductID,
        p.SKU,
        p.Name,
        p.BranchID,
        v.VariantID,
        pr.PriceID,
        pr.BranchID AS PriceBranchID,
        pr.SellingPrice
    FROM Demo_Retail_Product p
    LEFT JOIN Demo_Retail_Variant v ON p.ProductID = v.ProductID
    LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = 4
    WHERE p.SKU = 'SRN-STA-3TI'
    ORDER BY p.ProductID
""")

print("\nSKU 'SRN-STA-3TI' structure:")
print("ProductID | SKU | Name | ProductBranchID | VariantID | PriceID | PriceBranchID | SellingPrice")
for row in cursor.fetchall():
    print(f"{row[0]} | {row[1]} | {row[2][:20]} | {row[3]} | {row[4]} | {row[5]} | {row[6]} | {row[7]}")

# Check how CategoryNavigationService should work
print("\n" + "="*80)
print("\nCorrect query structure (without p.BranchID filter):")
cursor.execute("""
    SELECT TOP 10
        p.ProductID,
        p.SKU,
        p.Name,
        c.CategoryName,
        sc.SubCategoryName,
        pr.SellingPrice,
        pr.BranchID AS PriceBranchID
    FROM Demo_Retail_Product p
    INNER JOIN Categories c ON c.CategoryID = p.CategoryID
    INNER JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
    LEFT JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 4
    WHERE p.CategoryID = 17
        AND p.IsActive = 1
        AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
    ORDER BY p.Name
""")

print("ProductID | SKU | Name | Category | Subcategory | Price | PriceBranch")
for row in cursor.fetchall():
    print(f"{row[0]} | {row[1]} | {row[2][:30]} | {row[3]} | {row[4]} | {row[5]} | {row[6]}")

cursor.close()
conn.close()
