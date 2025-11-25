import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Checking Bonaqua products in database...")
print()

cursor.execute("""
    SELECT SKU, Name, Category, ProductType, IsActive, BranchID, Barcode 
    FROM Demo_Retail_Product 
    WHERE Name LIKE '%Bonaqua%' OR SKU LIKE '%BON%' OR SKU LIKE '%BAA%' OR SKU LIKE '%BAN%'
""")

rows = cursor.fetchall()
print(f"Found {len(rows)} Bonaqua products:")
print()

for r in rows:
    print(f"SKU: {r[0]:20} Name: {r[1]:40}")
    print(f"  Category: {str(r[2] or 'None'):15} Type: {str(r[3] or 'None'):10} Active: {r[4]} Branch: {r[5]} Barcode: {r[6] or 'None'}")
    print()

# Check if they meet POS criteria
print("\nChecking POS filter criteria:")
cursor.execute("""
    SELECT SKU, Name, IsActive, ProductType, Category,
        CASE WHEN IsActive = 1 THEN 'PASS' ELSE 'FAIL' END AS IsActive_Check,
        CASE WHEN (ProductType = 'External' OR ProductType = 'Internal') THEN 'PASS' ELSE 'FAIL' END AS ProductType_Check,
        CASE WHEN Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control') THEN 'PASS' ELSE 'FAIL' END AS Category_Check
    FROM Demo_Retail_Product 
    WHERE Name LIKE '%Bonaqua%' OR SKU LIKE '%BON%' OR SKU LIKE '%BAA%' OR SKU LIKE '%BAN%'
""")

rows = cursor.fetchall()
for r in rows:
    print(f"\n{r[0]} - {r[1]}")
    print(f"  IsActive={r[2]} [{r[5]}]")
    print(f"  ProductType={r[3]} [{r[6]}]")
    print(f"  Category={r[4]} [{r[7]}]")
    
    if r[5] == 'PASS' and r[6] == 'PASS' and r[7] == 'PASS':
        print(f"  ✓ SHOULD SHOW IN POS")
    else:
        print(f"  ✗ WILL NOT SHOW IN POS")

conn.close()
