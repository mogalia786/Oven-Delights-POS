"""
Check branch-specific pricing structure
"""

import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Checking branch-specific pricing...")
print()

# Check how many branches exist
cursor.execute("SELECT BranchID, BranchName FROM Branches ORDER BY BranchID")
branches = cursor.fetchall()

print(f"Found {len(branches)} branches:")
for branch in branches:
    print(f"  Branch {branch[0]}: {branch[1]}")

print()

# Check price distribution by branch
cursor.execute("""
    SELECT 
        CASE WHEN BranchID IS NULL THEN 'Global (NULL)' ELSE CAST(BranchID AS VARCHAR) END AS Branch,
        COUNT(*) AS PriceCount
    FROM Demo_Retail_Price
    GROUP BY BranchID
    ORDER BY BranchID
""")

print("Price records by branch:")
for row in cursor.fetchall():
    print(f"  {row[0]:20} {row[1]:,} prices")

print()

# Check if products have branch-specific prices
cursor.execute("""
    SELECT TOP 10
        p.SKU,
        p.Name,
        pr.BranchID,
        pr.SellingPrice
    FROM Demo_Retail_Product p
    INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID
    WHERE pr.BranchID IS NOT NULL
    ORDER BY p.Name
""")

branch_prices = cursor.fetchall()

if len(branch_prices) > 0:
    print(f"Sample products with branch-specific prices:")
    for row in branch_prices:
        print(f"  {row[0]:20} {row[1]:40} Branch {row[2]}: R {row[3]:.2f}")
else:
    print("⚠ NO products have branch-specific prices!")
    print("All prices are global (BranchID = NULL)")

print()

# Check Bonaqua pricing
cursor.execute("""
    SELECT p.SKU, p.Name, pr.BranchID, pr.SellingPrice
    FROM Demo_Retail_Product p
    INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID
    WHERE p.Name LIKE '%Bonaqua%'
    ORDER BY p.Name, pr.BranchID
""")

print("Bonaqua pricing by branch:")
for row in cursor.fetchall():
    branch = f"Branch {row[2]}" if row[2] else "Global"
    print(f"  {row[0]:20} {row[1]:40} {branch:15} R {row[3]:.2f}")

conn.close()

print()
print("="*70)
print("RECOMMENDATION:")
print("If you need different prices per branch, you should:")
print("1. Keep global prices (BranchID = NULL) as fallback")
print("2. Add branch-specific prices where needed")
print("3. POS will use branch-specific price if exists, else global price")
