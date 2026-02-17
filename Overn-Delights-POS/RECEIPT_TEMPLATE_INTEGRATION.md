# Receipt Template Integration - POS to ERP Designer

## Overview
The POS system now reads receipt field positions from the **existing ERP Receipt Template Designer**. When you adjust field positions in the ERP Utilities, the changes automatically apply to POS receipts.

## How It Works

### ERP Side (Design)
1. Open **ERP → Utilities → Receipt Template Designer**
2. Drag and position fields on the preview
3. Click **Save Configuration**
4. Positions saved to `ReceiptTemplateConfig` table

### POS Side (Print)
1. POS reads field positions from `ReceiptTemplateConfig` table
2. Uses configured X/Y positions for each field
3. Applies font sizes and bold settings
4. Prints receipt with your custom layout

## Database Tables Used

### ReceiptTemplateConfig (Existing)
Stores field positions configured in ERP Designer:
- `BranchID` - Which branch this template is for
- `FieldName` - Field identifier (e.g., 'CompanyName', 'InvoiceNumber')
- `XPosition` - Horizontal position in pixels
- `YPosition` - Vertical position in pixels
- `FontSize` - Font size (7-12)
- `IsBold` - Bold text (0/1)
- `IsEnabled` - Show/hide field (0/1)

### ContinuousPrinterConfig (Existing)
Stores printer settings:
- `BranchID` - Which branch
- `PrinterName` - Printer name from Windows
- `PrinterPath` - Network path or local name
- `PaperWidth` - Paper width in mm
- `IsActive` - Enabled (0/1)

## Field Names Available

### Header Fields
- `CompanyName` - "OVEN DELIGHTS"
- `CompanyTagline` - "YOUR TRUSTED FAMILY BAKERY"
- `CoRegNo` - Company registration number
- `VATNumber` - VAT number
- `ShopNo` - Shop number
- `Address` - Branch address
- `City` - City name
- `Phone` - Phone and fax
- `Email` - Email address

### Customer Fields
- `AccountNo` - Customer account number
- `CustomerName` - Customer name
- `Telephone` - Telephone number
- `CellNumber` - Cell phone number

### Order Fields
- `CakeColour` - Cake color
- `CakePicture` - Cake picture reference
- `CollectionDate` - Collection date
- `CollectionDay` - Day of week
- `CollectionTime` - Collection time
- `SpecialRequest` - Special instructions

### Transaction Fields
- `OrderHeader` - Column headers for order
- `OrderDetails` - Order line details
- `ItemHeader` - Item column headers
- `ItemLine1` - First item line
- `Message` - Custom message (e.g., "HAPPY BIRTHDAY")

### Totals Fields
- `InvoiceTotal` - Total amount
- `DepositPaid` - Deposit amount
- `BalanceOwing` - Balance due

### Footer Fields
- `Terms` - Terms and conditions line 1
- `Terms2` - Terms and conditions line 2

## Workflow

### 1. Setup (One Time)
```sql
-- Run in ERP database
-- Tables already exist from ERP setup
SELECT * FROM ReceiptTemplateConfig WHERE BranchID = 1;
SELECT * FROM ContinuousPrinterConfig WHERE BranchID = 1;
```

### 2. Design Template (In ERP)
1. Login to ERP
2. Go to **Utilities → Receipt Template Designer**
3. Select branch
4. Drag fields to desired positions
5. Click **Save**

### 3. Configure Printer (In ERP or SQL)
```sql
-- Set printer for branch
UPDATE ContinuousPrinterConfig
SET PrinterName = 'Star TSP143',
    PrinterPath = '\\SERVER\Star_TSP143'
WHERE BranchID = 1;
```

### 4. Test Print (In POS)
1. Make a sale or create an order in POS
2. Receipt prints automatically
3. Fields appear at positions you configured

## Adjusting Field Positions

### Method 1: ERP Designer (Recommended)
1. Open Receipt Template Designer
2. Click and drag fields
3. Save

### Method 2: Direct SQL (Advanced)
```sql
-- Move a field down 20 pixels
UPDATE ReceiptTemplateConfig
SET YPosition = YPosition + 20
WHERE FieldName = 'CompanyName' AND BranchID = 1;

-- Move a field right 50 pixels
UPDATE ReceiptTemplateConfig
SET XPosition = XPosition + 50
WHERE FieldName = 'InvoiceTotal' AND BranchID = 1;

-- Make a field bold
UPDATE ReceiptTemplateConfig
SET IsBold = 1, FontSize = 10
WHERE FieldName = 'BalanceOwing' AND BranchID = 1;

-- Hide a field
UPDATE ReceiptTemplateConfig
SET IsEnabled = 0
WHERE FieldName = 'VATNumber' AND BranchID = 1;
```

## Troubleshooting

### Receipt prints but fields are in wrong positions
- Check if template exists for your branch:
  ```sql
  SELECT * FROM ReceiptTemplateConfig WHERE BranchID = 1;
  ```
- If empty, open ERP Designer and save a template

### Receipt doesn't print at all
- Check printer configuration:
  ```sql
  SELECT * FROM ContinuousPrinterConfig WHERE BranchID = 1;
  ```
- Verify printer name matches Windows printer name exactly

### Fields are missing
- Check if field is enabled:
  ```sql
  SELECT FieldName, IsEnabled FROM ReceiptTemplateConfig WHERE BranchID = 1;
  ```
- Enable field:
  ```sql
  UPDATE ReceiptTemplateConfig SET IsEnabled = 1 WHERE FieldName = 'YourField';
  ```

## Benefits

✅ **No Code Changes** - Adjust positions without recompiling  
✅ **Branch-Specific** - Different layouts per branch  
✅ **Visual Designer** - Drag-and-drop in ERP  
✅ **Instant Updates** - Changes apply immediately  
✅ **Database-Driven** - All settings in one place  

## Files Modified

### POS Project
- `Services/POSReceiptPrinter.vb` - Updated to read from ReceiptTemplateConfig
- `Forms/POSMainForm.vb` - Calls printer after sale
- `Forms/CustomerOrderDialog.vb` - Calls printer after order

### ERP Project (Already Exists)
- `Forms/Utilities/ReceiptTemplateDesigner.vb` - Visual designer
- `SQL/CREATE_RECEIPT_TEMPLATE_TABLE.sql` - Table creation

## Next Steps

1. ✅ Run `SETUP_POS_PRINTER.sql` to configure printer
2. ✅ Open ERP Receipt Template Designer
3. ✅ Position fields as needed
4. ✅ Save configuration
5. ✅ Test print from POS
6. ✅ Adjust and repeat as needed
