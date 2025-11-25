"""
Final verification of all products, categories, barcodes, and prices
"""

import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("="*70)
print("FINAL VERIFICATION - ALL PRODUCTS")
print("="*70)

# 1. Total active retail products
cursor.execute("""
    SELECT COUNT(*)
    FROM Demo_Retail_Product
    WHERE IsActive = 1
    AND ProductType IN ('External', 'Internal')
    AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
""")
total_products = cursor.fetchone()[0]
print(f"\n1. Total active retail products: {total_products:,}")

# 2. Products with CategoryID
cursor.execute("""
    SELECT COUNT(*)
    FROM Demo_Retail_Product
    WHERE IsActive = 1
    AND ProductType IN ('External', 'Internal')
    AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    AND CategoryID IS NOT NULL
""")
with_category = cursor.fetchone()[0]
without_category = total_products - with_category
print(f"\n2. CategoryID Status:")
print(f"   ✓ With CategoryID: {with_category:,}")
print(f"   ✗ Without CategoryID: {without_category:,}")

# 3. Products with barcodes
cursor.execute("""
    SELECT COUNT(*)
    FROM Demo_Retail_Product
    WHERE IsActive = 1
    AND ProductType IN ('External', 'Internal')
    AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    AND Barcode IS NOT NULL AND Barcode <> ''
""")
with_barcode = cursor.fetchone()[0]
without_barcode = total_products - with_barcode
print(f"\n3. Barcode Status:")
print(f"   ✓ With Barcode: {with_barcode:,}")
print(f"   ✗ Without Barcode: {without_barcode:,}")

# 4. Products with prices for Branch 6
cursor.execute("""
    SELECT COUNT(DISTINCT p.ProductID)
    FROM Demo_Retail_Product p
    INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 6
    WHERE p.IsActive = 1
    AND p.ProductType IN ('External', 'Internal')
    AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
""")
with_price_b6 = cursor.fetchone()[0]
without_price_b6 = total_products - with_price_b6
print(f"\n4. Price Status (Branch 6):")
print(f"   ✓ With Price: {with_price_b6:,}")
print(f"   ✗ Without Price: {without_price_b6:,}")

# 5. Products ready for POS (has CategoryID AND price for Branch 6)
cursor.execute("""
    SELECT COUNT(DISTINCT p.ProductID)
    FROM Demo_Retail_Product p
    INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 6
    WHERE p.IsActive = 1
    AND p.ProductType IN ('External', 'Internal')
    AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    AND p.CategoryID IS NOT NULL
""")
pos_ready = cursor.fetchone()[0]
print(f"\n5. POS Ready (CategoryID + Price):")
print(f"   ✓ Ready for POS: {pos_ready:,}")
print(f"   ✗ Not ready: {total_products - pos_ready:,}")

# 6. Sample products NOT ready
if total_products - pos_ready > 0:
    print(f"\n6. Sample products NOT ready for POS:")
    cursor.execute("""
        SELECT TOP 10 p.SKU, p.Name, p.CategoryID, 
               CASE WHEN pr.ProductID IS NULL THEN 'NO PRICE' ELSE 'HAS PRICE' END AS PriceStatus
        FROM Demo_Retail_Product p
        LEFT JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 6
        WHERE p.IsActive = 1
        AND p.ProductType IN ('External', 'Internal')
        AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
        AND (p.CategoryID IS NULL OR pr.ProductID IS NULL)
    """)
    
    for row in cursor.fetchall():
        cat_id = row[2] if row[2] else "NO CATEGORY"
        print(f"   {row[0]:20} {row[1]:40} CatID: {cat_id:12} {row[3]}")

# 7. All branches price coverage
print(f"\n7. Price coverage across all branches:")
for branch_id in [1, 3, 4, 5, 6, 8, 9, 10, 11]:
    cursor.execute("""
        SELECT COUNT(DISTINCT p.ProductID)
        FROM Demo_Retail_Product p
        INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = ?
        WHERE p.IsActive = 1
        AND p.ProductType IN ('External', 'Internal')
        AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
    """, branch_id)
    
    branch_count = cursor.fetchone()[0]
    coverage = (branch_count / total_products * 100) if total_products > 0 else 0
    print(f"   Branch {branch_id:2}: {branch_count:,} products ({coverage:.1f}%)")

print("\n" + "="*70)
print("SUMMARY")
print("="*70)
print(f"Total Products: {total_products:,}")
print(f"POS Ready: {pos_ready:,} ({(pos_ready/total_products*100):.1f}%)")
print(f"With Barcodes: {with_barcode:,} ({(with_barcode/total_products*100):.1f}%)")
print("="*70)

if pos_ready == total_products:
    print("✓ ALL PRODUCTS ARE READY FOR POS!")
else:
    print(f"⚠ {total_products - pos_ready} products still need CategoryID or prices")

conn.close()
