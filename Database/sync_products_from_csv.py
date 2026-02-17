"""
Sync products from CSV to database
- Find missing products in database
- Add missing products to Demo_Retail_Product
- Update barcodes for all products
"""

import pyodbc
import csv
import sys

# Fix encoding for Windows console
if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

# Database connection settings
SERVER = 'tcp:mogalia.database.windows.net,1433'
DATABASE = 'Oven_Delights_Main'
USER = 'faroq786'
PASSWORD = 'Faroq#786'
CONNECTION_STRING = f'DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={SERVER};DATABASE={DATABASE};UID={USER};PWD={PASSWORD};Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

# CSV file path
CSV_FILE = r'C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\Copy of ITEM LIST NEW 2025 updated.csv'

def read_csv_products():
    """Read all products from CSV"""
    print("Reading CSV file...")
    products = []
    
    with open(CSV_FILE, 'r', encoding='utf-8') as file:
        csv_reader = csv.DictReader(file)
        for row in csv_reader:
            item_code = row['ITEM CODE'].strip() if 'ITEM CODE' in row else row.get('I+1:1579TEM CCODE', '').strip()
            barcode = row.get('BARCODE', '').strip()
            description = row.get('ITEM DESCRIPTION', '').strip()
            sub_category = row.get('Sub Category', '').strip()
            main_category = row.get('Main Category', '').strip()
            item_category = row.get('item catergory', '').strip()
            uom = row.get('unit of measure', '').strip()
            
            if item_code:
                products.append({
                    'ItemCode': item_code,
                    'Barcode': barcode if barcode and barcode != '0' else None,
                    'Description': description,
                    'SubCategory': sub_category,
                    'MainCategory': main_category,
                    'ItemCategory': item_category,
                    'UOM': uom
                })
    
    print(f"✓ Found {len(products)} products in CSV")
    return products

def get_existing_skus():
    """Get all existing SKUs from database"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        cursor.execute("SELECT SKU FROM Demo_Retail_Product")
        existing_skus = set(row[0] for row in cursor.fetchall())
        
        cursor.close()
        conn.close()
        
        print(f"✓ Found {len(existing_skus)} existing products in database")
        return existing_skus
        
    except Exception as e:
        print(f"✗ Error reading database: {e}")
        return set()

def get_categories_and_subcategories():
    """Get existing categories and subcategories"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        # Get categories
        cursor.execute("SELECT CategoryID, CategoryName FROM Categories")
        categories = {row[1].lower().strip(): row[0] for row in cursor.fetchall()}
        
        # Get subcategories
        cursor.execute("SELECT SubCategoryID, SubCategoryName FROM SubCategories")
        subcategories = {row[1].lower().strip(): row[0] for row in cursor.fetchall()}
        
        cursor.close()
        conn.close()
        
        return categories, subcategories
        
    except Exception as e:
        print(f"✗ Error reading categories: {e}")
        return {}, {}

def find_missing_products(csv_products, existing_skus):
    """Find products in CSV that don't exist in database"""
    missing = []
    for product in csv_products:
        if product['ItemCode'] not in existing_skus:
            missing.append(product)
    
    print(f"✓ Found {len(missing)} missing products")
    return missing

def add_missing_products(missing_products, categories, subcategories):
    """Add missing products to database"""
    if not missing_products:
        print("No missing products to add")
        return 0
    
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        added_count = 0
        skipped_count = 0
        
        print(f"\nAdding {len(missing_products)} missing products...")
        
        # Default category/subcategory IDs (adjust as needed)
        default_category_id = 1
        default_subcategory_id = 1
        
        for product in missing_products:
            try:
                # Map category
                category_name = product['MainCategory'].lower().strip()
                category_id = categories.get(category_name, default_category_id)
                
                # Map subcategory
                subcategory_name = product['SubCategory'].lower().strip()
                subcategory_id = subcategories.get(subcategory_name, default_subcategory_id)
                
                # Determine product type
                item_category = product['ItemCategory'].lower()
                product_type = 'Internal' if 'internal' in item_category else 'External'
                
                # Insert product
                cursor.execute("""
                    INSERT INTO Demo_Retail_Product 
                    (SKU, Name, Description, CategoryID, SubCategoryID, Category, ProductType, 
                     Barcode, IsActive, BranchID, CurrentStock)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, 1, 1, 0)
                """, 
                    product['ItemCode'],
                    product['Description'],
                    product['Description'],
                    category_id,
                    subcategory_id,
                    product['MainCategory'],
                    product_type,
                    product['Barcode']
                )
                
                added_count += 1
                
                if added_count % 50 == 0:
                    print(f"  Added {added_count} products...")
                    conn.commit()
                    
            except Exception as e:
                skipped_count += 1
                if 'duplicate' not in str(e).lower():
                    print(f"  ⚠ Error adding {product['ItemCode']}: {e}")
        
        conn.commit()
        cursor.close()
        conn.close()
        
        print(f"\n✓ Successfully added {added_count} products")
        if skipped_count > 0:
            print(f"⚠ Skipped {skipped_count} products (errors or duplicates)")
        
        return added_count
        
    except Exception as e:
        print(f"✗ Error adding products: {e}")
        return 0

def update_all_barcodes(csv_products):
    """Update barcodes for all products"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        updated_count = 0
        
        print(f"\nUpdating barcodes for all products...")
        
        for product in csv_products:
            if product['Barcode']:
                cursor.execute("""
                    UPDATE Demo_Retail_Product 
                    SET Barcode = ? 
                    WHERE SKU = ?
                """, product['Barcode'], product['ItemCode'])
                
                if cursor.rowcount > 0:
                    updated_count += 1
                
                if updated_count % 100 == 0:
                    print(f"  Updated {updated_count} barcodes...")
        
        conn.commit()
        cursor.close()
        conn.close()
        
        print(f"✓ Updated {updated_count} barcodes")
        return updated_count
        
    except Exception as e:
        print(f"✗ Error updating barcodes: {e}")
        return 0

def main():
    """Main execution"""
    print("="*50)
    print("PRODUCT SYNC UTILITY")
    print("="*50)
    print()
    
    # Step 1: Read CSV
    csv_products = read_csv_products()
    if not csv_products:
        return
    
    print()
    
    # Step 2: Get existing products
    existing_skus = get_existing_skus()
    
    print()
    
    # Step 3: Find missing products
    missing_products = find_missing_products(csv_products, existing_skus)
    
    if missing_products:
        print()
        print("Sample missing products:")
        for product in missing_products[:10]:
            print(f"  {product['ItemCode']:20} - {product['Description']}")
        
        print()
        print(f"Adding {len(missing_products)} missing products to database...")
        
        # Get categories
        categories, subcategories = get_categories_and_subcategories()
        
        # Add missing products
        add_missing_products(missing_products, categories, subcategories)
    
    print()
    
    # Step 4: Update barcodes for all products
    print("Updating barcodes for all products...")
    update_all_barcodes(csv_products)
    
    print()
    print("✓ Product sync completed!")

if __name__ == "__main__":
    main()
