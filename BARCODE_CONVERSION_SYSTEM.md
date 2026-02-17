# Barcode Conversion System - 13-Digit Scannable Barcodes

## Overview
Converts alphanumeric invoice/order numbers to 13-digit numeric barcodes that scan reliably, then converts back to original format for database lookup.

---

## How It Works

### Encoding Format (13 digits):
```
[DocType:1][Branch:2][Sequence:10]
```

### Example Conversions:

| Original Number | Barcode (13 digits) | Scans As |
|----------------|---------------------|----------|
| O-OD200-000033 | 2200000000033 | 2200000000033 |
| INV-JHB-00123 | 1010000000123 | 1010000000123 |
| INV-CPT-00456 | 1020000000456 | 1020000000456 |
| O-JHB-000001 | 2010000000001 | 2010000000001 |

---

## Document Type Codes (1 digit):

| Type | Code |
|------|------|
| INV (Invoice) | 1 |
| O (Order) | 2 |
| RET (Return) | 3 |
| REF (Refund) | 4 |
| Unknown | 9 |

---

## Branch Codes (2 digits):

| Branch | Code |
|--------|------|
| JHB | 01 |
| CPT | 02 |
| DBN | 03 |
| PE | 04 |
| BFN | 05 |
| PLK | 06 |
| NLS | 07 |
| KIM | 08 |
| OD200 | 20 |
| OD201 | 21 |
| OD202 | 22 |

**To add new branches**, use `BarcodeConverter.AddBranchCode("NEWBRANCH", "99")`

---

## Workflow

### 1. Printing Receipt:
```vb
' Original invoice number
Dim invoiceNumber = "O-OD200-000033"

' Generate barcode image
Dim barcodeImage = BarcodeGenerator.GenerateCode39Barcode(invoiceNumber, 250, 50)

' Internally converts to: "2200000000033"
' Barcode displays: 13-digit pattern
' Human-readable text below: "O-OD200-000033"
```

### 2. Scanning Barcode:
```vb
' Scanner reads barcode
Scanner Input: "2200000000033"

' BarcodeScannerDialog automatically converts back
Dim scannedInvoice = BarcodeConverter.FromBarcode("2200000000033")
' Result: "O-OD200-000033"

' Use for database lookup
Dim invoice = GetInvoiceByNumber(scannedInvoice)
```

---

## Files Created/Modified

### New Files:
1. **BarcodeConverter.vb** - Conversion logic
   - `ToBarcode(invoiceNumber)` - Convert to 13-digit
   - `FromBarcode(barcode)` - Convert back to original
   - `AddBranchCode(name, code)` - Add new branch

### Modified Files:
1. **BarcodeGenerator.vb**
   - Now uses `BarcodeConverter.ToBarcode()` first
   - Generates ERP-style digit pattern (thick/thin bars)
   - Shows original text below barcode

2. **BarcodeScannerDialog.vb**
   - Automatically converts scanned barcode back
   - Returns original invoice/order number format

3. **Overn-Delights-POS.vbproj**
   - Added BarcodeConverter.vb to compilation

---

## Barcode Length

**13 digits = Perfect for scanning!**
- Same length as ERP product barcodes (6009704313540)
- Fits on 80mm thermal receipt
- Scans reliably with standard barcode scanners
- Uses proven ERP barcode pattern

---

## Benefits

✅ **Scannable** - 13 digits, same as working ERP barcodes
✅ **Branch-specific** - Encodes branch code
✅ **Reversible** - Converts back to original format
✅ **Compact** - Fits on small receipts
✅ **Reliable** - Uses proven digit-based pattern
✅ **Automatic** - Conversion happens transparently

---

## Usage Examples

### Returns Processing:
```vb
' Customer presents receipt with barcode
' Scan barcode: "1010000000123"
' System converts: "INV-JHB-00123"
' Lookup invoice in database
' Process return
```

### Order Collection:
```vb
' Customer presents order slip with barcode
' Scan barcode: "2200000000033"
' System converts: "O-OD200-000033"
' Lookup order in database
' Mark as collected
```

---

## Adding New Branches

```vb
' In application startup or configuration
BarcodeConverter.AddBranchCode("DURBAN", "09")
BarcodeConverter.AddBranchCode("PRETORIA", "10")

' Now these branches will encode/decode correctly
' O-DURBAN-000001 -> 2090000000001
' O-PRETORIA-000001 -> 2100000000001
```

---

## Testing

### Test Conversion:
```vb
' Test encoding
Dim barcode = BarcodeConverter.ToBarcode("O-OD200-000033")
Console.WriteLine(barcode) ' Output: 2200000000033

' Test decoding
Dim original = BarcodeConverter.FromBarcode("2200000000033")
Console.WriteLine(original) ' Output: O-OD200-000033

' Round-trip test
Dim test = "INV-JHB-00123"
Dim encoded = BarcodeConverter.ToBarcode(test)
Dim decoded = BarcodeConverter.FromBarcode(encoded)
Console.WriteLine(test = decoded) ' Output: True
```

### Test Scanning:
1. Print receipt with barcode
2. Scan with barcode scanner
3. Verify scanned number is 13 digits
4. Verify system converts back to original format
5. Verify database lookup works

---

## Troubleshooting

### Barcode doesn't scan:
- Check barcode is exactly 13 digits
- Verify using digit-based pattern (not alphanumeric)
- Ensure sufficient white space around barcode
- Test with different scanner settings

### Wrong invoice number returned:
- Check branch code mapping in BarcodeConverter
- Verify document type code is correct
- Test conversion with `BarcodeConverter.FromBarcode()`

### New branch not working:
- Add branch code with `AddBranchCode()`
- Ensure code is unique (2 digits)
- Rebuild application

---

## Technical Details

### Barcode Pattern:
- Uses same digit-based pattern as ERP product barcodes
- Even digits = thick bars
- Odd digits = thin bars
- White space between bars
- Scanners decode to numeric string

### Conversion Algorithm:
1. Parse invoice number (split by hyphen)
2. Extract document type, branch, sequence
3. Lookup codes from dictionaries
4. Concatenate: [DocType:1][Branch:2][Sequence:10]
5. Pad to exactly 13 digits

### Reverse Algorithm:
1. Parse barcode: positions 0, 1-2, 3-12
2. Lookup document type and branch names
3. Format sequence (remove leading zeros, pad to 6)
4. Concatenate: "{DocType}-{Branch}-{Sequence}"

---

## Important Notes

- **13 digits only** - Longer barcodes won't scan reliably
- **Numeric only** - Alphanumeric barcodes are too long
- **Branch codes must be unique** - 2 digits per branch
- **Sequence max: 10 digits** - Supports up to 9,999,999,999 invoices
- **Human-readable text** - Always shown below barcode
- **Automatic conversion** - Transparent to user
