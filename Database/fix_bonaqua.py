import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Fixing Bonaqua products...")

# Update ProductType for Bonaqua products
cursor.execute("""
    UPDATE Demo_Retail_Product 
    SET ProductType = 'External' 
    WHERE (Name LIKE '%Bonaqua%' OR SKU LIKE '%BAA%' OR SKU LIKE '%BAN-500%' OR SKU LIKE '%BON-500%') 
    AND Category = 'drink'
    AND (ProductType IS NULL OR ProductType = '')
""")

updated = cursor.rowcount
conn.commit()

print(f"✓ Updated {updated} Bonaqua products to ProductType='External'")

# Verify the fix
cursor.execute("""
    SELECT SKU, Name, ProductType, Category 
    FROM Demo_Retail_Product 
    WHERE Name LIKE '%Bonaqua%' AND Category = 'drink'
""")

print("\nBonaqua products after fix:")
for row in cursor.fetchall():
    print(f"  {row[0]:20} {row[1]:40} Type: {row[2]}")

conn.close()
print("\n✓ Bonaqua products should now appear in POS!")
