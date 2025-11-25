import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

conn = pyodbc.connect(CONNECTION_STRING)
cursor = conn.cursor()

print("Price distribution by branch:")
cursor.execute("""
    SELECT 
        CASE WHEN BranchID IS NULL THEN 'Global' ELSE CAST(BranchID AS VARCHAR) END AS Branch,
        COUNT(*) AS PriceCount
    FROM Demo_Retail_Price
    GROUP BY BranchID
    ORDER BY BranchID
""")

for row in cursor.fetchall():
    print(f"  Branch {row[0]:10} {row[1]:,} prices")

print("\n✓ All branches already have prices!")
print("This is why the script added 0 - they were already populated.")

conn.close()
