import pyodbc
import csv
from datetime import datetime

# Database connection string from App.config
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

print("Connecting to database...")
try:
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    print("Connected successfully!")
except Exception as e:
    print(f"Connection failed: {e}")
    print("\nPlease update the connection string in the script with your actual credentials.")
    exit(1)

# Read CSV file
csv_file = r'c:\Development Apps\Cascades projects\Overn-Delights-POS\Overn-Delights-POS\Database\OD400_Umhlanga_Prices.csv'
print(f"\nReading CSV file: {csv_file}")

products_to_update = []
products_to_insert = []
products_not_found = []

with open(csv_file, 'r', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    
    for row in reader:
        item_code = row['ItemCode']
        incl_price = float(row['InclPrice']) if row['InclPrice'] else 0
        cost = float(row['Cost']) if row['Cost'] else 0
        
        if incl_price > 0:
            # Check if product exists
            cursor.execute("SELECT ProductID FROM Demo_Retail_Product WHERE SKU = ? AND IsActive = 1", item_code)
            result = cursor.fetchone()
            
            if result:
                product_id = result[0]
                
                # Check if price exists for BranchID=4
                cursor.execute("""
                    SELECT TOP 1 PriceID 
                    FROM Demo_Retail_Price 
                    WHERE ProductID = ? AND BranchID = 4 
                    ORDER BY EffectiveFrom DESC
                """, product_id)
                
                price_result = cursor.fetchone()
                
                if price_result:
                    products_to_update.append((incl_price, cost, price_result[0]))
                else:
                    products_to_insert.append((product_id, 4, incl_price, cost))
            else:
                products_not_found.append((item_code, row['ItemDescription'], incl_price))

print(f"\nProducts to update: {len(products_to_update)}")
print(f"Products to insert: {len(products_to_insert)}")
print(f"Products not found: {len(products_not_found)}")

# Update existing prices
if products_to_update:
    print("\nUpdating existing prices...")
    cursor.executemany("""
        UPDATE Demo_Retail_Price 
        SET SellingPrice = ?, CostPrice = ?, EffectiveFrom = GETDATE() 
        WHERE PriceID = ?
    """, products_to_update)
    print(f"Updated {len(products_to_update)} prices")

# Insert new prices
if products_to_insert:
    print("\nInserting new prices...")
    cursor.executemany("""
        INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
        VALUES (?, ?, ?, ?, GETDATE())
    """, products_to_insert)
    print(f"Inserted {len(products_to_insert)} new prices")

# Commit changes
conn.commit()
print("\nChanges committed successfully!")

# Show products not found
if products_not_found:
    print("\n" + "="*80)
    print("Products not found in database:")
    print("-"*80)
    for item_code, desc, price in products_not_found[:20]:  # Show first 20
        print(f"  {item_code:20} | {desc:40} | R{price:8.2f}")
    if len(products_not_found) > 20:
        print(f"  ... and {len(products_not_found) - 20} more")

# Verify import
print("\n" + "="*80)
print("Verification - Umhlanga (BranchID=4) Price Summary:")
print("-"*80)
cursor.execute("""
    SELECT 
        COUNT(DISTINCT ProductID) AS TotalProducts,
        MIN(SellingPrice) AS MinPrice,
        MAX(SellingPrice) AS MaxPrice,
        AVG(SellingPrice) AS AvgPrice
    FROM Demo_Retail_Price
    WHERE BranchID = 4 AND SellingPrice > 0
""")
result = cursor.fetchone()
print(f"Total Products: {result[0]}")
print(f"Min Price: R{result[1]:.2f}")
print(f"Max Price: R{result[2]:.2f}")
print(f"Avg Price: R{result[3]:.2f}")

# Show sample of updated prices
print("\n" + "="*80)
print("Sample of updated prices:")
print("-"*80)
cursor.execute("""
    SELECT TOP 10
        p.SKU,
        p.Name,
        drp.SellingPrice,
        drp.CostPrice
    FROM Demo_Retail_Price drp
    INNER JOIN Demo_Retail_Product p ON drp.ProductID = p.ProductID
    WHERE drp.BranchID = 4 AND drp.SellingPrice > 0
    ORDER BY drp.EffectiveFrom DESC
""")
for row in cursor.fetchall():
    print(f"  {row[0]:20} | {row[1]:40} | R{row[2]:8.2f} | Cost: R{row[3]:8.2f}")

cursor.close()
conn.close()

print("\n" + "="*80)
print("OD400 (Umhlanga) Price Import Complete!")
print("="*80)
