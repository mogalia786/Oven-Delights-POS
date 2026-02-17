import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Testing if view was updated...")
print()

# Check view definition
cursor.execute("""
    SELECT OBJECT_DEFINITION(OBJECT_ID('vw_POS_Products'))
""")

view_def = cursor.fetchone()[0]

if 'p.CategoryID NOT IN' in view_def:
    print("✓ View has been updated with CategoryID filter")
elif 'p.Category NOT IN' in view_def:
    print("✗ View still uses old Category text filter")
    print("\nYou need to run FIX_POS_PRODUCTS_VIEW.sql in SSMS!")
else:
    print("? Cannot determine view filter type")

print("\n" + "="*70)
print("Testing view results for Beverages products:")
print("="*70)

cursor.execute("""
    SELECT COUNT(*)
    FROM vw_POS_Products
    WHERE BranchID = 6
""")

total = cursor.fetchone()[0]
print(f"\nTotal products in view for Branch 6: {total}")

# Check Beverages specifically
cursor.execute("""
    SELECT COUNT(*)
    FROM Demo_Retail_Product p
    WHERE p.CategoryID = 12  -- Beverages
    AND p.IsActive = 1
    AND p.BranchID = 6
""")

bev_in_table = cursor.fetchone()[0]
print(f"Beverages products in table (Branch 6): {bev_in_table}")

cursor.execute("""
    SELECT COUNT(*)
    FROM vw_POS_Products
    WHERE Category = 'Beverages'
    AND BranchID = 6
""")

bev_in_view = cursor.fetchone()[0]
print(f"Beverages products in view (Branch 6): {bev_in_view}")

if bev_in_view == 0 and bev_in_table > 0:
    print("\n✗ VIEW NOT UPDATED - Run FIX_POS_PRODUCTS_VIEW.sql!")
elif bev_in_view == bev_in_table:
    print("\n✓ View is correct - rebuild POS to see changes")
else:
    print(f"\n? Mismatch: {bev_in_table} in table vs {bev_in_view} in view")

conn.close()
