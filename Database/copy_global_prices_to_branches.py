"""
Copy global prices (BranchID = NULL) to all branches
This allows branch-specific pricing while ensuring all branches have prices
"""

import pyodbc
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONNECTION_STRING = 'DRIVER={ODBC Driver 17 for SQL Server};SERVER=tcp:mogalia.database.windows.net,1433;DATABASE=Oven_Delights_Main;UID=faroq786;PWD=Faroq#786;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;'

def copy_global_prices_to_branches():
    """Copy global prices to all branches"""
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        
        print("="*70)
        print("COPY GLOBAL PRICES TO ALL BRANCHES")
        print("="*70)
        print()
        
        # Get all active branches (exclude HEAD OFFICE if BranchID = 12)
        cursor.execute("""
            SELECT BranchID, BranchName 
            FROM Branches 
            WHERE BranchID NOT IN (12)
            ORDER BY BranchID
        """)
        
        branches = cursor.fetchall()
        print(f"Found {len(branches)} branches to update:")
        for branch in branches:
            print(f"  Branch {branch[0]}: {branch[1]}")
        
        print()
        
        # Get all global prices
        cursor.execute("""
            SELECT ProductID, SellingPrice, CostPrice, EffectiveFrom
            FROM Demo_Retail_Price
            WHERE BranchID IS NULL
        """)
        
        global_prices = cursor.fetchall()
        print(f"Found {len(global_prices)} global prices to copy")
        print()
        
        total_added = 0
        
        for branch in branches:
            branch_id = branch[0]
            branch_name = branch[1]
            
            print(f"Processing Branch {branch_id} ({branch_name})...")
            
            # Use a single INSERT statement with NOT EXISTS check
            cursor.execute("""
                INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
                SELECT 
                    gp.ProductID, 
                    ? AS BranchID, 
                    gp.SellingPrice, 
                    gp.CostPrice, 
                    gp.EffectiveFrom
                FROM Demo_Retail_Price gp
                WHERE gp.BranchID IS NULL
                AND NOT EXISTS (
                    SELECT 1 FROM Demo_Retail_Price bp
                    WHERE bp.ProductID = gp.ProductID 
                    AND bp.BranchID = ?
                )
            """, branch_id, branch_id)
            
            added_count = cursor.rowcount
            conn.commit()
            total_added += added_count
            print(f"  ✓ Added {added_count} prices to Branch {branch_id}")
        
        print()
        print("="*70)
        print(f"✓ COMPLETED!")
        print(f"  Total prices added: {total_added:,}")
        print("="*70)
        
        # Verify Bonaqua now has prices for all branches
        print()
        print("Verifying Bonaqua prices across all branches:")
        
        cursor.execute("""
            SELECT p.SKU, p.Name, pr.BranchID, pr.SellingPrice
            FROM Demo_Retail_Product p
            INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID
            WHERE p.Name LIKE '%Bonaqua%' AND p.Category = 'drink'
            ORDER BY p.Name, pr.BranchID
        """)
        
        for row in cursor.fetchall():
            branch = f"Branch {row[2]}" if row[2] else "Global"
            print(f"  {row[0]:20} {row[1]:40} {branch:15} R {row[3]:.2f}")
        
        cursor.close()
        conn.close()
        
        print()
        print("✓ All branches now have complete pricing!")
        print("You can now set different prices per branch in the ERP system.")
        
    except Exception as e:
        print(f"✗ Error: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    copy_global_prices_to_branches()
