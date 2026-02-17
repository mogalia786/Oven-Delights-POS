# Python Barcode Import Instructions

## Prerequisites

1. **Python 3.7 or higher** installed
2. **ODBC Driver 17 for SQL Server** installed

## Setup

### 1. Install Python Dependencies

Open Command Prompt or PowerShell in this directory and run:

```bash
pip install -r requirements.txt
```

This installs `pyodbc` for SQL Server connectivity.

### 2. Configure Database Connection

Edit `import_barcodes.py` and update these settings (lines 12-14):

```python
SERVER = 'localhost'  # Your SQL Server instance name
DATABASE = 'OvenDelightsERP'  # Your database name
```

**Common SQL Server instance names:**
- `localhost` or `.` - Default local instance
- `localhost\SQLEXPRESS` - SQL Express
- `YOUR-PC-NAME\SQLEXPRESS` - Named instance

### 3. Verify CSV File Path

The script expects the CSV at:
```
C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\Copy of ITEM LIST NEW 2025 updated.csv
```

If your file is elsewhere, update line 16 in `import_barcodes.py`.

## Run the Script

```bash
python import_barcodes.py
```

## What the Script Does

1. ✅ Checks if `Barcode` column exists in `Demo_Retail_Product`
2. ✅ Adds column if missing
3. ✅ Reads CSV file (skips header row)
4. ✅ Filters out empty barcodes and "0" values
5. ✅ Matches products by SKU (Item Code)
6. ✅ Updates `Barcode` column for matched products
7. ✅ Shows summary and sample results

## Expected Output

```
==================================================
BARCODE IMPORT UTILITY
==================================================

✓ Barcode column already exists
Reading CSV file: C:\...\Copy of ITEM LIST NEW 2025 updated.csv
✓ Found 450 items with barcodes in CSV

Updating 450 products...
  Updated 50 products...
  Updated 100 products...
  ...
✓ Successfully updated 445 products
⚠ 5 products not found in database

==================================================
SUMMARY
==================================================
Products with barcodes:    445
Products without barcodes: 1134
==================================================

Sample products with barcodes:
  BIS-CHD-EAC          Biscuit Choc Delights 300G        → 14079
  BIS-CJN-EAC          Biscuit Jam Nest 300G             → 14067
  CAN-MAG-24S          Candle Magic Assorted             → 8004
  ...

✓ Barcode import completed successfully!
```

## Troubleshooting

### Error: "ODBC Driver not found"
Install ODBC Driver 17:
https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server

### Error: "Login failed"
- Check SQL Server allows Windows Authentication
- Or update script to use SQL Authentication:
  ```python
  CONNECTION_STRING = f'DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={SERVER};DATABASE={DATABASE};UID=your_username;PWD=your_password;'
  ```

### Error: "CSV file not found"
- Verify file path in script
- Use raw string (r'...') for Windows paths

### Products not found in database
- Check SKU values match exactly (case-sensitive)
- Verify products exist in `Demo_Retail_Product` table

## After Import

1. Run `FIX_POS_PRODUCTS_VIEW.sql` to update the view
2. Rebuild POS application
3. Test barcode display on product cards
