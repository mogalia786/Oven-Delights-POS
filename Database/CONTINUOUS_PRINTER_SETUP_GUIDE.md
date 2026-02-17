# Continuous Printer Setup Guide

## Overview
The POS system now supports **dual printing**:
1. **Thermal Slip Printer** (80mm Epson) - Default printer, prints immediately
2. **Continuous Network Printer** - Configured per branch with XY field positioning

## Database Setup

### Step 1: Create Tables
Run the SQL script to create the printer configuration tables:
```sql
-- Run this script
CREATE_CONTINUOUS_PRINTER_TABLE.sql
```

This creates:
- `ContinuousPrinterConfig` - Printer settings per branch
- `ContinuousPrinterFields` - XY coordinates for each field

### Step 2: Configure Printer for Your Branch

```sql
-- Update printer name for Branch 6 (example)
UPDATE ContinuousPrinterConfig 
SET PrinterName = '\\192.168.1.100\KitchenPrinter',  -- Your actual network printer
    PrinterIPAddress = '192.168.1.100',
    PaperWidth = 210,  -- mm (A4 width)
    PaperHeight = 297  -- mm (A4 height)
WHERE BranchID = 6
```

### Step 3: Configure Field Positions

The field positions are in **millimeters** from the top-left corner of the paper.

**Example: Adjust positions for your layout**
```sql
-- Update Invoice Number position
UPDATE ContinuousPrinterFields
SET XPosition = 15,  -- 15mm from left
    YPosition = 40   -- 40mm from top
WHERE BranchID = 6 AND FieldName = 'InvoiceNumber'

-- Update Total position
UPDATE ContinuousPrinterFields
SET XPosition = 130,
    YPosition = 225,
    FontSize = 14,
    IsBold = 1
WHERE BranchID = 6 AND FieldName = 'Total'
```

## Field Names Reference

| Field Name | Description | Example Value |
|------------|-------------|---------------|
| `StoreName` | Company name | "OVEN DELIGHTS" |
| `BranchName` | Branch location | "Umhlanga" |
| `InvoiceNumber` | Invoice/receipt number | "Invoice: INV-001234" |
| `Date` | Sale date | "Date: 25/11/2025" |
| `Time` | Sale time | "Time: 14:30:45" |
| `TillNumber` | Till/register number | "Till: 1" |
| `CashierName` | Cashier name | "Cashier: John Doe" |
| `ItemsHeader` | Column headers for items | "Item    Qty    Price    Total" |
| `LineItemStart` | Starting position for line items | (dynamic) |
| `Subtotal` | Subtotal amount | "Subtotal: R 150.00" |
| `Tax` | Tax amount | "Tax (15%): R 22.50" |
| `Total` | Total amount | "TOTAL: R 172.50" |
| `PaymentMethod` | Payment type | "Payment: CASH" |
| `CashTendered` | Cash amount given | "Cash: R 200.00" |
| `Change` | Change amount | "CHANGE: R 27.50" |
| `ThankYou` | Thank you message | "Thank you for your purchase!" |

## How It Works

### 1. Thermal Printer (Default)
- Prints to Windows default printer
- 80mm width thermal receipt
- Automatic paper cutting
- No configuration needed

### 2. Continuous Printer (Network)
- Reads printer name from database
- Reads field positions from database
- Prints each field at exact XY coordinates
- Supports custom fonts and sizes per field

### 3. Printing Flow
```
Sale Complete
    ↓
Print to Thermal Printer (80mm slip)
    ↓
Print to Continuous Printer (network)
    ↓
Show Receipt on Screen
```

## Testing

### Test Thermal Printer
1. Complete a sale in POS
2. Check if receipt prints to default printer
3. Verify 80mm width and proper formatting

### Test Continuous Printer
1. Ensure network printer is accessible
2. Complete a sale in POS
3. Check if receipt prints to network printer
4. Verify field positions are correct
5. Adjust XY coordinates in database if needed

## Troubleshooting

### Thermal Printer Not Printing
- Check Windows default printer is set correctly
- Verify printer is online and has paper
- Check printer drivers are installed

### Continuous Printer Not Printing
```sql
-- Check if printer is configured
SELECT * FROM ContinuousPrinterConfig WHERE BranchID = 6

-- Check if fields are configured
SELECT * FROM ContinuousPrinterFields WHERE BranchID = 6 ORDER BY YPosition
```

### Fields Not Aligned
1. Print a test receipt
2. Measure actual positions with a ruler
3. Update XY coordinates in database:
```sql
UPDATE ContinuousPrinterFields
SET XPosition = [new_x_mm],
    YPosition = [new_y_mm]
WHERE BranchID = 6 AND FieldName = '[field_name]'
```

## Coordinate System

```
(0,0) ← Top-Left Corner
  ↓
  X = Horizontal (left to right) in mm
  Y = Vertical (top to bottom) in mm

Example A4 Paper (210mm x 297mm):
┌─────────────────────────┐
│ (0,0)                   │
│                         │
│    (50,100)             │
│       ↓                 │
│    [Field Here]         │
│                         │
│                         │
│                  (210,297)
└─────────────────────────┘
```

## Adding New Fields

```sql
INSERT INTO ContinuousPrinterFields (
    BranchID, 
    FieldName, 
    XPosition, 
    YPosition, 
    FontName, 
    FontSize, 
    IsBold, 
    MaxWidth
)
VALUES (
    6,                    -- Your branch ID
    'CustomField',        -- Field name
    50,                   -- X position in mm
    150,                  -- Y position in mm
    'Arial',              -- Font name
    12,                   -- Font size
    1,                    -- Bold (1=yes, 0=no)
    100                   -- Max width in mm
)
```

Then update `DualReceiptPrinter.vb` to handle the new field in the `GetFieldValue` function.

## Network Printer Path Examples

### Windows Network Printer
```
\\192.168.1.100\KitchenPrinter
\\SERVERNAME\PrinterName
```

### IP Address Direct
```
192.168.1.100
```

## Support

For issues or questions:
1. Check printer is accessible from Windows
2. Verify database configuration
3. Check application logs for errors
4. Test with Microsoft Print to PDF first
