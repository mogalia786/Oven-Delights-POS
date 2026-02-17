"""
Set default prices for products that have no price or R 0.00
"""

import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

DEFAULT_PRICE = 10.00  # Default selling price for products without prices

def set_default_prices():
    """Set default prices for products without prices"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        print("Finding products without prices...")
        
        # Find products without any price records
        cursor.execute("""
            SELECT p.ProductID, p.SKU, p.Name
            FROM Demo_Retail_Product p
            WHERE p.IsActive = 1
            AND p.ProductType IN ('External', 'Internal')
            AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
            AND NOT EXISTS (
                SELECT 1 FROM Demo_Retail_Price pr 
                WHERE pr.ProductID = p.ProductID
            )
        """)
        
        products_without_prices = cursor.fetchall()
        
        print(f"Found {len(products_without_prices)} products without prices")
        
        if len(products_without_prices) > 0:
            print(f"\nAdding default price of R {DEFAULT_PRICE:.2f} to {len(products_without_prices)} products...")
            
            for product in products_without_prices:
                cursor.execute("""
                    INSERT INTO Demo_Retail_Price 
                    (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
                    VALUES (?, NULL, ?, 0.00, GETDATE())
                """, product[0], DEFAULT_PRICE)
            
            conn.commit()
            print(f"✓ Added default prices to {len(products_without_prices)} products")
        
        # Also update products that have R 0.00 price
        cursor.execute("""
            UPDATE Demo_Retail_Price
            SET SellingPrice = ?
            WHERE SellingPrice = 0.00
            AND ProductID IN (
                SELECT ProductID FROM Demo_Retail_Product
                WHERE IsActive = 1
                AND ProductType IN ('External', 'Internal')
                AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
            )
        """, DEFAULT_PRICE)
        
        zero_price_updated = cursor.rowcount
        conn.commit()
        
        if zero_price_updated > 0:
            print(f"✓ Updated {zero_price_updated} products from R 0.00 to R {DEFAULT_PRICE:.2f}")
        
        # Verify Bonaqua
        cursor.execute("""
            SELECT p.SKU, p.Name, 
                   (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
                    WHERE ProductID = p.ProductID 
                    ORDER BY EffectiveFrom DESC) AS Price
            FROM Demo_Retail_Product p
            WHERE p.Name LIKE '%Bonaqua%' AND p.Category = 'drink'
        """)
        
        print("\n\nBonaqua products after setting default prices:")
        for row in cursor.fetchall():
            price = f"R {row[2]:.2f}" if row[2] else "NO PRICE"
            print(f"  {row[0]:20} {row[1]:40} {price}")
        
        cursor.close()
        conn.close()
        
        print("\n✓ All products now have prices!")
        print(f"NOTE: Default price of R {DEFAULT_PRICE:.2f} was set.")
        print("Please update actual prices in the ERP system.")
        
    except Exception as e:
        print(f"✗ Error: {e}")

if __name__ == "__main__":
    set_default_prices()
