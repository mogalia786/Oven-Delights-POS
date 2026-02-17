# Dual Printing Implementation - Using Existing ERP Tables

## ✅ Updated Implementation

The POS now uses the **existing printer configuration tables** from the ERP system instead of creating new ones.

### Existing Database Tables (from ERP)

**1. `PrinterConfig`** - Stores printer settings per branch
- Created in ERP → Utilities → Continuous Printer Setup
- Columns: BranchID, PrinterName, PrinterIPAddress

**2. `ReceiptTemplateConfig`** - Stores field positions per branch
- Created in ERP → Utilities → Continuous Printer Setup (Receipt Template Designer)
- Columns: BranchID, FieldName, XPosition, YPosition, FontSize, IsBold, IsEnabled

### How It Works

1. **Configure in ERP**
   - Open ERP → Utilities → Continuous Printer Setup
   - Use the Receipt Template Designer to position fields
   - Set printer name and IP address
   - Save configuration

2. **POS Reads Configuration**
   - When a sale completes, POS reads from `PrinterConfig` and `ReceiptTemplateConfig`
   - Prints to thermal printer (default)
   - Prints to continuous printer using configured positions

3. **No Database Scripts Needed**
   - Tables already exist in ERP database
   - Configuration is done through ERP UI
   - POS just reads the existing configuration

## 🔧 Setup Steps

### 1. Configure Printer in ERP
1. Open **Oven Delights ERP**
2. Go to **Utilities → Continuous Printer Setup**
3. Use the **Receipt Template Designer** to:
   - Position all receipt fields (drag and drop)
   - Set font sizes and bold options
   - Configure printer name (e.g., `\\192.168.1.100\KitchenPrinter`)
4. Click **Save Configuration**

### 2. Test in POS
1. Complete a sale in POS
2. Receipt prints to:
   - **Thermal printer** (80mm slip) - default Windows printer
   - **Continuous printer** (network) - using ERP configuration

### 3. Adjust if Needed
1. If fields are misaligned, go back to ERP
2. Open Receipt Template Designer
3. Adjust field positions
4. Save and test again in POS

## 📋 Field Mapping

The POS maps receipt data to the following field names (configured in ERP):

| Field Name (ERP) | POS Data | Example |
|------------------|----------|---------|
| `CompanyName` | "OVEN DELIGHTS" | Fixed |
| `AccountNo` | Invoice Number | "INV-001234" |
| `CollectionDate` | Sale Date | "25/11/2025" |
| `CollectionTime` | Sale Time | "14:30:45" |
| `ItemLine1` | First line item | Starting position for items |
| `InvoiceTotal` | Total Amount | "R 172.50" |
| `DepositPaid` | Cash Tendered | "R 200.00" |
| `BalanceOwing` | Change | "R 27.50" |

## 🎯 Key Benefits

### ✅ No Duplicate Tables
- Uses existing ERP infrastructure
- Single source of truth for printer config
- No need to sync between systems

### ✅ Visual Configuration
- Receipt Template Designer provides drag-and-drop interface
- See exactly how receipt will look
- No manual XY coordinate entry

### ✅ Per-Branch Support
- Each branch can have different printer
- Each branch can have different layout
- Managed centrally in ERP

### ✅ Easy Maintenance
- Change printer name in one place (ERP)
- Adjust layout visually
- Changes apply immediately to POS

## 🔍 Troubleshooting

### Continuous Printer Not Printing

**Check ERP Configuration:**
```sql
-- Check if printer is configured for your branch
SELECT * FROM PrinterConfig WHERE BranchID = 6

-- Check if fields are configured
SELECT * FROM ReceiptTemplateConfig WHERE BranchID = 6 AND IsEnabled = 1
```

**If No Configuration:**
1. Open ERP → Utilities → Continuous Printer Setup
2. Configure printer and fields
3. Save configuration
4. Test again in POS

### Fields Misaligned

1. Open ERP → Utilities → Continuous Printer Setup
2. Use Receipt Template Designer to adjust positions
3. Save configuration
4. Test in POS

### Printer Not Found

1. Verify printer name in ERP matches actual printer
2. Check network connectivity to printer
3. Test printer from Windows first
4. Update printer name in ERP if needed

## 📚 Files Modified

1. **Services\DualReceiptPrinter.vb**
   - Updated to read from `PrinterConfig` table
   - Updated to read from `ReceiptTemplateConfig` table
   - Removed references to non-existent tables

2. **Forms\PaymentTenderForm.vb**
   - Calls `PrintReceiptDual()` after sale completion
   - Passes all receipt data to dual printer service

## ⚠️ Important Notes

- **DO NOT** run `CREATE_CONTINUOUS_PRINTER_TABLE.sql` - tables already exist
- **DO** use ERP Receipt Template Designer to configure
- **Positions are in pixels**, not millimeters
- **Paper size is fixed** at 220mm x 297mm (A4 continuous)

---

**Updated**: November 25, 2025  
**Status**: ✅ Complete - Uses Existing ERP Tables
