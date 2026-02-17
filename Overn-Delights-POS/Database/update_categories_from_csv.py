"""
Update CategoryID and SubCategoryID from CSV file
Maps products to correct categories so they appear in category navigation
"""

import pyodbc
import csv
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

CSV_FILE = r'C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\Copy of ITEM LIST NEW 2025 updated.csv'

def get_category_mappings(cursor):
    """Get CategoryID mappings from database"""
    cursor.execute("SELECT CategoryID, CategoryName FROM Categories")
    categories = {row[1].strip().lower(): row[0] for row in cursor.fetchall()}
    
    cursor.execute("SELECT SubCategoryID, SubCategoryName FROM SubCategories")
    subcategories = {row[1].strip().lower(): row[0] for row in cursor.fetchall()}
    
    return categories, subcategories

def read_csv_categories():
    """Read category info from CSV"""
    print("Reading categories from CSV...")
    product_categories = {}
    
    with open(CSV_FILE, 'r', encoding='utf-8') as file:
        csv_reader = csv.reader(file)
        next(csv_reader)  # Skip header
        
        for row in csv_reader:
            if len(row) >= 6:
                item_code = row[0].strip()
                sub_category = row[3].strip() if len(row) > 3 else ''
                main_category = row[4].strip() if len(row) > 4 else ''
                
                if item_code:
                    product_categories[item_code] = {
                        'sub_category': sub_category,
                        'main_category': main_category
                    }
    
    print(f"✓ Found {len(product_categories)} products with category info")
    return product_categories

def update_categories(cursor, product_categories, categories, subcategories):
    """Update CategoryID and SubCategoryID for products"""
    updated_count = 0
    not_found_categories = set()
    not_found_subcategories = set()
    
    print("\nUpdating product categories...")
    
    for item_code, cat_info in product_categories.items():
        main_cat = cat_info['main_category'].strip().lower()
        sub_cat = cat_info['sub_category'].strip().lower()
        
        # Get CategoryID
        category_id = categories.get(main_cat)
        if not category_id:
            # Try variations
            for cat_name, cat_id in categories.items():
                if main_cat in cat_name or cat_name in main_cat:
                    category_id = cat_id
                    break
        
        # Get SubCategoryID
        subcategory_id = subcategories.get(sub_cat)
        if not subcategory_id:
            # Try variations
            for subcat_name, subcat_id in subcategories.items():
                if sub_cat in subcat_name or subcat_name in sub_cat:
                    subcategory_id = subcat_id
                    break
        
        # Update if we found IDs
        if category_id:
            cursor.execute("""
                UPDATE Demo_Retail_Product
                SET CategoryID = ?, SubCategoryID = ?
                WHERE SKU = ?
            """, category_id, subcategory_id if subcategory_id else 1, item_code)
            
            if cursor.rowcount > 0:
                updated_count += 1
                
                if updated_count % 50 == 0:
                    print(f"  Updated {updated_count} products...")
        else:
            not_found_categories.add(main_cat)
        
        if not subcategory_id and sub_cat:
            not_found_subcategories.add(sub_cat)
    
    print(f"\n✓ Updated {updated_count} products with CategoryID/SubCategoryID")
    
    if not_found_categories:
        print(f"\n⚠ Categories not found in database: {', '.join(not_found_categories)}")
    
    if not_found_subcategories:
        print(f"⚠ SubCategories not found in database: {', '.join(not_found_subcategories)}")
    
    return updated_count

def verify_bonaqua(cursor):
    """Verify Bonaqua now has correct CategoryID"""
    cursor.execute("""
        SELECT p.SKU, p.Name, p.CategoryID, p.SubCategoryID, c.CategoryName, sc.SubCategoryName
        FROM Demo_Retail_Product p
        LEFT JOIN Categories c ON c.CategoryID = p.CategoryID
        LEFT JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
        WHERE p.Name LIKE '%Bonaqua%'
    """)
    
    print("\n\nBonaqua category verification:")
    for row in cursor.fetchall():
        cat_id = row[2] if row[2] else "NULL"
        subcat_id = row[3] if row[3] else "NULL"
        cat_name = row[4] if row[4] else "N/A"
        subcat_name = row[5] if row[5] else "N/A"
        print(f"  {row[0]:20} CatID: {cat_id:3} ({cat_name:15}) SubCatID: {subcat_id:3} ({subcat_name})")

def main():
    """Main execution"""
    print("="*70)
    print("UPDATE CATEGORIES FROM CSV")
    print("="*70)
    print()
    
    conn = pyodbc.connect(CONNECTION_STRING)
    cursor = conn.cursor()
    
    # Get category mappings from database
    categories, subcategories = get_category_mappings(cursor)
    print(f"Found {len(categories)} categories and {len(subcategories)} subcategories in database")
    
    # Read CSV
    product_categories = read_csv_categories()
    
    # Update products
    update_categories(cursor, product_categories, categories, subcategories)
    
    conn.commit()
    
    # Verify
    verify_bonaqua(cursor)
    
    cursor.close()
    conn.close()
    
    print("\n✓ Category update completed!")
    print("Rebuild POS to see products in category navigation.")

if __name__ == "__main__":
    main()
