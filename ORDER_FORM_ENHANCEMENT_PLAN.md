# Order Form Enhancement Plan

## Requirements Summary
1. ✅ **Larger Form**: 1200x750 (from 1000x620)
2. ✅ **Cell Number First**: Reorder customer fields
3. ✅ **Collection Day**: Auto-populate from date (e.g., "Saturday")
4. ✅ **Customer Table**: POS_Customers with auto-lookup
5. ✅ **Taller Special Instructions**: 100px height (from 60px)
6. ⏳ **Dual Printing**: Customer slip + Teller slip
7. ⏳ **Continuous Feed Printer**: Epson LX-350 template
8. ⏳ **ERP Utility**: Setup Continuous Printer form

## Database Changes
- **POS_Customers** table created
- **ContinuousPrinterConfig** table created

## Form Changes Needed
### CustomerOrderDialog.Designer.vb
- Form size: 1200x750
- Customer Details fields reordered:
  1. Cell Number (with lookup)
  2. Name
  3. Surname  
  4. Email (optional)
- Order Details:
  1. Ready Date
  2. Collection Day (readonly, auto-filled)
  3. Time
  4. Special Instructions (taller)

### CustomerOrderDialog.vb
- Add cell number TextChanged event for customer lookup
- Add date ValueChanged event for day of week
- Add print methods:
  - PrintCustomerSlip()
  - PrintTellerSlip()
  - PrintContinuousFeed()

## Printing Requirements
### Regular Printer (2 slips)
- Customer Copy: Order details, items, deposit, balance
- Teller Copy: Same + internal notes

### Continuous Feed (Epson LX-350)
- Network printer path from config
- Template layout matching pink slip image
- Fields: Branch, Account, Name, Cell, Special Request, Collection Point, Order Number, Date, Items, Totals

## ERP Utility Form
- Location: Utilities menu
- Name: Setup Continuous Printer
- Fields:
  - Branch selection
  - Printer Name
  - Network Path
  - Test Print button
  - Save button

## Next Steps
1. Complete Designer.vb control initialization
2. Implement customer lookup logic
3. Implement day of week auto-fill
4. Create print templates
5. Create ERP utility form
