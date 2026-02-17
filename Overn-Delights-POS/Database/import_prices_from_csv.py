"""
Import prices from MASTER_PRODUCT_LIST.csv
Updates Demo_Retail_Price table with SellingPrice and CostPrice
"""

import pyodbc
import csv
import sys
from datetime import datetime

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

CSV_FILE = r'C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\MASTER_PRODUCT_LIST.csv'

def read_prices_from_csv():
    """Read prices from CSV"""
    print("Reading prices from CSV...")
    prices = []
    
    with open(CSV_FILE, 'r', encoding='utf-8-sig') as file:
        csv_reader = csv.DictReader(file, quotechar='"')
        for row in csv_reader:
            item_code = row.get('ItemCode', row.get('"ItemCode"', '')).strip()
            selling_price = row.get('SellingPrice', '0').strip()
            cost_price = row.get('CostPrice', '0').strip()
            
            # Convert to float
            try:
                selling_price = float(selling_price) if selling_price else 0.0
                cost_price = float(cost_price) if cost_price else 0.0
            except:
                selling_price = 0.0
                cost_price = 0.0
            
            if item_code and (selling_price > 0 or cost_price > 0):
                prices.append({
                    'ItemCode': item_code,
                    'SellingPrice': selling_price,
                    'CostPrice': cost_price
                })
    
    print(f"✓ Found {len(prices)} products with prices in CSV")
    return prices

def import_prices(prices):
    """Import prices into Demo_Retail_Price table"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        added_count = 0
        updated_count = 0
        not_found_count = 0
        
        print(f"\nImporting prices for {len(prices)} products...")
        
        for price_data in prices:
            # Get ProductID from SKU
            cursor.execute("""
                SELECT TOP 1 ProductID 
                FROM Demo_Retail_Product 
                WHERE SKU = ?
            """, price_data['ItemCode'])
            
            result = cursor.fetchone()
            
            if result:
                product_id = result[0]
                
                # Check if price already exists
                cursor.execute("""
                    SELECT COUNT(*) 
                    FROM Demo_Retail_Price 
                    WHERE ProductID = ? AND BranchID IS NULL
                """, product_id)
                
                exists = cursor.fetchone()[0] > 0
                
                if exists:
                    # Update existing price
                    cursor.execute("""
                        UPDATE Demo_Retail_Price 
                        SET SellingPrice = ?, 
                            CostPrice = ?,
                            EffectiveFrom = GETDATE()
                        WHERE ProductID = ? 
                        AND BranchID IS NULL
                    """, price_data['SellingPrice'], price_data['CostPrice'], product_id)
                    updated_count += 1
                else:
                    # Insert new price
                    cursor.execute("""
                        INSERT INTO Demo_Retail_Price 
                        (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
                        VALUES (?, NULL, ?, ?, GETDATE())
                    """, product_id, price_data['SellingPrice'], price_data['CostPrice'])
                    added_count += 1
                
                if (added_count + updated_count) % 100 == 0:
                    print(f"  Processed {added_count + updated_count} prices...")
                    conn.commit()
            else:
                not_found_count += 1
        
        conn.commit()
        cursor.close()
        conn.close()
        
        print(f"\n✓ Added {added_count} new prices")
        print(f"✓ Updated {updated_count} existing prices")
        if not_found_count > 0:
            print(f"⚠ {not_found_count} products not found in database")
        
        return added_count + updated_count
        
    except Exception as e:
        print(f"✗ Error importing prices: {e}")
        return 0

def verify_bonaqua_prices():
    """Verify Bonaqua now has prices"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT p.SKU, p.Name, 
                   (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
                    WHERE ProductID = p.ProductID 
                    ORDER BY EffectiveFrom DESC) AS Price
            FROM Demo_Retail_Product p
            WHERE p.Name LIKE '%Bonaqua%' AND p.Category = 'drink'
        """)
        
        print("\n\nBonaqua products after price import:")
        for row in cursor.fetchall():
            price = f"R {row[2]:.2f}" if row[2] else "NO PRICE"
            print(f"  {row[0]:20} {row[1]:40} {price}")
        
        conn.close()
        
    except Exception as e:
        print(f"Error checking Bonaqua: {e}")

def main():
    """Main execution"""
    print("="*70)
    print("PRICE IMPORT UTILITY")
    print("="*70)
    print()
    
    # Read prices from CSV
    prices = read_prices_from_csv()
    
    if not prices:
        print("No prices found in CSV")
        return
    
    # Import prices
    import_prices(prices)
    
    # Verify Bonaqua
    verify_bonaqua_prices()
    
    print("\n✓ Price import completed!")
    print("Rebuild POS to see updated prices.")

if __name__ == "__main__":
    main()
