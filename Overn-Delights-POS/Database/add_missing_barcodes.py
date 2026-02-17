"""
Add barcodes to products that don't have them
Uses MASTER_PRODUCT_LIST.csv as source
"""

import pyodbc
import csv
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

CSV_FILE = r'C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\MASTER_PRODUCT_LIST.csv'

def read_barcodes_from_csv():
    """Read barcodes from CSV"""
    print("Reading barcodes from MASTER_PRODUCT_LIST.csv...")
    barcodes = {}
    
    with open(CSV_FILE, 'r', encoding='utf-8-sig') as file:
        csv_reader = csv.DictReader(file, quotechar='"')
        for row in csv_reader:
            item_code = row.get('ItemCode', '').strip()
            barcode = row.get('Barcode', '').strip()
            
            # Clean barcode (remove .0 from numbers like "16001.0")
            if barcode and barcode != '0':
                barcode = barcode.replace('.0', '')
                barcodes[item_code] = barcode
    
    print(f"✓ Found {len(barcodes)} barcodes in CSV")
    return barcodes

def update_missing_barcodes(barcodes):
    """Update products that don't have barcodes"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        print("\nUpdating products with missing barcodes...")
        
        updated_count = 0
        not_found_count = 0
        
        for item_code, barcode in barcodes.items():
            # Update products that don't have a barcode
            cursor.execute("""
                UPDATE Demo_Retail_Product 
                SET Barcode = ? 
                WHERE SKU = ? 
                AND (Barcode IS NULL OR Barcode = '')
            """, barcode, item_code)
            
            if cursor.rowcount > 0:
                updated_count += cursor.rowcount
                
                if updated_count % 50 == 0:
                    print(f"  Updated {updated_count} products...")
                    conn.commit()
            else:
                # Check if product exists but already has barcode
                cursor.execute("""
                    SELECT Barcode FROM Demo_Retail_Product WHERE SKU = ?
                """, item_code)
                
                result = cursor.fetchone()
                if not result:
                    not_found_count += 1
        
        conn.commit()
        cursor.close()
        conn.close()
        
        print(f"\n✓ Updated {updated_count} products with barcodes")
        if not_found_count > 0:
            print(f"⚠ {not_found_count} products from CSV not found in database")
        
        return updated_count
        
    except Exception as e:
        print(f"✗ Error: {e}")
        return 0

def verify_barcodes():
    """Show summary of barcode coverage"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        # Count products with and without barcodes
        cursor.execute("""
            SELECT 
                COUNT(CASE WHEN Barcode IS NOT NULL AND Barcode <> '' THEN 1 END) AS WithBarcode,
                COUNT(CASE WHEN Barcode IS NULL OR Barcode = '' THEN 1 END) AS WithoutBarcode
            FROM Demo_Retail_Product
            WHERE IsActive = 1
            AND ProductType IN ('External', 'Internal')
            AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
        """)
        
        result = cursor.fetchone()
        
        print("\n" + "="*70)
        print("BARCODE COVERAGE SUMMARY")
        print("="*70)
        print(f"Products with barcodes:    {result[0]:,}")
        print(f"Products without barcodes: {result[1]:,}")
        print("="*70)
        
        # Show Bonaqua specifically
        cursor.execute("""
            SELECT SKU, Name, Barcode
            FROM Demo_Retail_Product
            WHERE Name LIKE '%Bonaqua%' AND Category = 'drink'
        """)
        
        print("\nBonaqua products:")
        for row in cursor.fetchall():
            barcode = row[2] if row[2] else "NO BARCODE"
            print(f"  {row[0]:20} {row[1]:40} {barcode}")
        
        cursor.close()
        conn.close()
        
    except Exception as e:
        print(f"Error: {e}")

def main():
    """Main execution"""
    print("="*70)
    print("ADD MISSING BARCODES")
    print("="*70)
    print()
    
    # Read barcodes from CSV
    barcodes = read_barcodes_from_csv()
    
    if not barcodes:
        print("No barcodes found in CSV")
        return
    
    # Update missing barcodes
    update_missing_barcodes(barcodes)
    
    # Show summary
    verify_barcodes()
    
    print("\n✓ Barcode update completed!")

if __name__ == "__main__":
    main()
