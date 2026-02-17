import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Checking for products with NULL ProductType...")

# Count products with NULL ProductType
cursor.execute("""
    SELECT COUNT(*) 
    FROM Demo_Retail_Product 
    WHERE (ProductType IS NULL OR ProductType = '')
    AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
""")

null_count = cursor.fetchone()[0]
print(f"Found {null_count} products with NULL ProductType")

if null_count > 0:
    print("\nSample products with NULL ProductType:")
    cursor.execute("""
        SELECT TOP 20 SKU, Name, Category 
        FROM Demo_Retail_Product 
        WHERE (ProductType IS NULL OR ProductType = '')
        AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    """)
    
    for row in cursor.fetchall():
        print(f"  {row[0]:20} {row[1]:50} Cat: {row[2]}")
    
    print(f"\nUpdating {null_count} products to ProductType='External'...")
    
    # Update all NULL ProductTypes to External (since they're finished products for sale)
    cursor.execute("""
        UPDATE Demo_Retail_Product 
        SET ProductType = 'External' 
        WHERE (ProductType IS NULL OR ProductType = '')
        AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    """)
    
    updated = cursor.rowcount
    conn.commit()
    
    print(f"✓ Updated {updated} products to ProductType='External'")
else:
    print("✓ All products have ProductType set!")

# Also update ingredients/raw materials to have ProductType if NULL
cursor.execute("""
    SELECT COUNT(*) 
    FROM Demo_Retail_Product 
    WHERE (ProductType IS NULL OR ProductType = '')
    AND Category IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
""")

raw_null_count = cursor.fetchone()[0]

if raw_null_count > 0:
    print(f"\nFound {raw_null_count} raw materials with NULL ProductType")
    print("Updating to ProductType='External' (purchased materials)...")
    
    cursor.execute("""
        UPDATE Demo_Retail_Product 
        SET ProductType = 'External' 
        WHERE (ProductType IS NULL OR ProductType = '')
        AND Category IN ('ingredients', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    """)
    
    cursor.execute("""
        UPDATE Demo_Retail_Product 
        SET ProductType = 'Internal' 
        WHERE (ProductType IS NULL OR ProductType = '')
        AND Category IN ('sub recipe')
    """)
    
    conn.commit()
    print(f"✓ Updated raw materials ProductType")

conn.close()
print("\n✓ All products now have ProductType set and should appear in POS!")
