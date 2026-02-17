# Barcode Import and Display Setup

## Step 1: Import CSV Data

**File Location:**
```
C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\Copy of ITEM LIST NEW 2025 updated.csv
```

**CSV Structure:**
- Column 1: ITEM CODE (matches SKU in database)
- Column 2: BARCODE
- Column 3: ITEM DESCRIPTION
- Columns 4-7: Category info

## Step 2: Run SQL Scripts (in order)

### 2.1 Import Barcodes
Run: `IMPORT_BARCODES_FROM_CSV.sql`

This script will:
1. Create temp table `#ItemBarcodes`
2. Add `Barcode` column to `Demo_Retail_Product` (if not exists)
3. Import CSV data (you need to use SQL Import Wizard or BULK INSERT)
4. Update products with barcodes from CSV (matching by SKU)

**Manual Import Options:**

**Option A: SQL Server Import/Export Wizard**
1. Right-click database → Tasks → Import Data
2. Select CSV file
3. Target table: `#ItemBarcodes`
4. Map columns correctly
5. Run the UPDATE section of the script

**Option B: BULK INSERT (if permissions allow)**
```sql
BULK INSERT #ItemBarcodes
FROM 'C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\Copy of ITEM LIST NEW 2025 updated.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    TABLOCK
)
```

### 2.2 Update POS View
Run: `FIX_POS_PRODUCTS_VIEW.sql`

This recreates the `vw_POS_Products` view to include:
- Barcode column
- CurrentStock (not stock tables)
- SKU as ItemCode

## Step 3: Rebuild POS Application

After running both scripts, rebuild the POS solution.

## What Changes Were Made

### Database Changes:
1. ✅ Added `Barcode` column to `Demo_Retail_Product`
2. ✅ Populated barcodes from CSV (matching by SKU)
3. ✅ Updated `vw_POS_Products` view to include Barcode

### Code Changes:
1. ✅ `CategoryNavigationService.vb` - Query includes Barcode
2. ✅ `POSMainForm_REDESIGN.vb` - Product tiles display barcode
3. ✅ `CreateProductTileNew` function - Added barcode parameter and label

### Product Card Layout:
```
┌─────────────────────┐
│ CEX-MBS-EAC         │ ← Item Code (SKU)
│ 🔖 14079            │ ← Barcode (if exists)
│                     │
│   Product Name      │ ← Center
│                     │
│ R 40.00  Stock: 49  │ ← Price (left), Stock (right)
└─────────────────────┘
```

## Verification

After rebuild, check:
1. Category browse shows barcodes on product cards
2. Name search shows barcodes
3. Products without barcodes show only SKU (no barcode line)
4. Stock quantities display correctly
5. Sales reduce CurrentStock properly

## Notes

- Products without barcodes in CSV (empty or "0") will not show barcode label
- Barcode displays below item code in small gray text with 🔖 icon
- Barcode is clickable (adds product to cart)
- Barcode participates in hover effects
