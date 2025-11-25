"""
Import barcodes from CSV and update Demo_Retail_Product table
Matches by SKU (Item Code) and updates Barcode column
"""

import pyodbc
import csv
import os
import sys

# Fix encoding for Windows console
if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

# Database connection settings
SERVER = 'tcp:mogalia.database.windows.net,1433'
DATABASE = 'Oven_Delights_Main'
USER = 'faroq786'
PASSWORD = 'Faroq#786'
# Azure SQL Database connection
CONNECTION_STRING = f'DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={SERVER};DATABASE={DATABASE};UID={USER};PWD={PASSWORD};Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

# CSV file path
CSV_FILE = r'C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\Copy of ITEM LIST NEW 2025 updated.csv'

def create_barcode_column():
    """Add Barcode column to Demo_Retail_Product if it doesn't exist"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        # Check if column exists
        cursor.execute("""
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = 'Demo_Retail_Product' 
            AND COLUMN_NAME = 'Barcode'
        """)
        
        if cursor.fetchone()[0] == 0:
            print("Adding Barcode column to Demo_Retail_Product...")
            cursor.execute("ALTER TABLE Demo_Retail_Product ADD Barcode NVARCHAR(50) NULL")
            conn.commit()
            print("✓ Barcode column added successfully")
        else:
            print("✓ Barcode column already exists")
        
        cursor.close()
        conn.close()
        return True
        
    except Exception as e:
        print(f"✗ Error creating column: {e}")
        return False

def read_csv_file():
    """Read CSV file and return list of (ItemCode, Barcode) tuples"""
    if not os.path.exists(CSV_FILE):
        print(f"✗ CSV file not found: {CSV_FILE}")
        return []
    
    print(f"Reading CSV file: {CSV_FILE}")
    barcodes = []
    
    try:
        with open(CSV_FILE, 'r', encoding='utf-8') as file:
            csv_reader = csv.reader(file)
            next(csv_reader)  # Skip header row
            
            for row in csv_reader:
                if len(row) >= 2:
                    item_code = row[0].strip()
                    barcode = row[1].strip()
                    
                    # Only include if barcode exists and is not "0"
                    if barcode and barcode != '0':
                        barcodes.append((item_code, barcode))
        
        print(f"✓ Found {len(barcodes)} items with barcodes in CSV")
        return barcodes
        
    except Exception as e:
        print(f"✗ Error reading CSV: {e}")
        return []

def update_barcodes(barcodes):
    """Update Demo_Retail_Product with barcodes from CSV"""
    if not barcodes:
        print("No barcodes to update")
        return
    
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        updated_count = 0
        not_found_count = 0
        
        print(f"\nUpdating {len(barcodes)} products...")
        
        for item_code, barcode in barcodes:
            # Check if product exists
            cursor.execute("""
                SELECT COUNT(*) 
                FROM Demo_Retail_Product 
                WHERE SKU = ?
            """, item_code)
            
            if cursor.fetchone()[0] > 0:
                # Update barcode
                cursor.execute("""
                    UPDATE Demo_Retail_Product 
                    SET Barcode = ? 
                    WHERE SKU = ?
                """, barcode, item_code)
                updated_count += 1
                
                if updated_count % 50 == 0:
                    print(f"  Updated {updated_count} products...")
            else:
                not_found_count += 1
                print(f"  ⚠ Product not found: {item_code}")
        
        conn.commit()
        cursor.close()
        conn.close()
        
        print(f"\n✓ Successfully updated {updated_count} products")
        if not_found_count > 0:
            print(f"⚠ {not_found_count} products not found in database")
        
        return True
        
    except Exception as e:
        print(f"✗ Error updating barcodes: {e}")
        return False

def show_summary():
    """Show summary of products with and without barcodes"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        # Count products with barcodes
        cursor.execute("""
            SELECT COUNT(*) 
            FROM Demo_Retail_Product 
            WHERE Barcode IS NOT NULL AND Barcode <> ''
        """)
        with_barcode = cursor.fetchone()[0]
        
        # Count products without barcodes
        cursor.execute("""
            SELECT COUNT(*) 
            FROM Demo_Retail_Product 
            WHERE Barcode IS NULL OR Barcode = ''
        """)
        without_barcode = cursor.fetchone()[0]
        
        print("\n" + "="*50)
        print("SUMMARY")
        print("="*50)
        print(f"Products with barcodes:    {with_barcode}")
        print(f"Products without barcodes: {without_barcode}")
        print("="*50)
        
        # Show sample of products with barcodes
        print("\nSample products with barcodes:")
        cursor.execute("""
            SELECT TOP 10 SKU, Name, Barcode 
            FROM Demo_Retail_Product 
            WHERE Barcode IS NOT NULL AND Barcode <> ''
            ORDER BY Name
        """)
        
        for row in cursor.fetchall():
            print(f"  {row.SKU:20} {row.Name:40} → {row.Barcode}")
        
        cursor.close()
        conn.close()
        
    except Exception as e:
        print(f"✗ Error showing summary: {e}")

def main():
    """Main execution function"""
    print("="*50)
    print("BARCODE IMPORT UTILITY")
    print("="*50)
    print()
    
    # Step 1: Create Barcode column
    if not create_barcode_column():
        return
    
    print()
    
    # Step 2: Read CSV file
    barcodes = read_csv_file()
    if not barcodes:
        return
    
    print()
    
    # Step 3: Update database
    if not update_barcodes(barcodes):
        return
    
    # Step 4: Show summary
    show_summary()
    
    print("\n✓ Barcode import completed successfully!")

if __name__ == "__main__":
    main()
