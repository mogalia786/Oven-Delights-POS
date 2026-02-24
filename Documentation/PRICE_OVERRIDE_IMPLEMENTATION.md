# Line Item Price Override Feature

## Overview
Allows retail supervisors to override line item prices for wholesale customers.

## Features
- ✅ Supervisor authentication required
- ✅ 4-decimal precision pricing (e.g., R 15.0002)
- ✅ Custom numeric keypad dialog
- ✅ Visual indicators for overridden prices
- ✅ Auto-recalculation of totals
- ✅ Full audit trail
- ✅ Does NOT affect Demo_Retail_Price table

## Database Schema
```sql
-- POS_SaleItems
ALTER TABLE POS_SaleItems ADD OverriddenPrice DECIMAL(18,4) NULL
ALTER TABLE POS_SaleItems ADD PriceOverrideBy NVARCHAR(50) NULL
ALTER TABLE POS_SaleItems ADD PriceOverrideDate DATETIME NULL

-- POS_CustomOrderItems
ALTER TABLE POS_CustomOrderItems ADD OverriddenPrice DECIMAL(18,4) NULL
ALTER TABLE POS_CustomOrderItems ADD PriceOverrideBy NVARCHAR(50) NULL
ALTER TABLE POS_CustomOrderItems ADD PriceOverrideDate DATETIME NULL
```

## UI Components
1. **Price Override Button** - In cart grid, next to quantity
2. **PriceOverrideDialog** - Custom numeric keypad with R symbol
3. **Visual Indicator** - Gold background for overridden prices

## Workflow
1. User clicks price override button on line item
2. Retail supervisor authentication dialog
3. Price entry dialog with numeric keypad
4. Confirmation and validation
5. Price updated, totals recalculated
6. Visual indicator applied

## Files Modified
- POSMainForm.vb - Added price override button and logic
- CakeOrderFormNew.vb - Added price override for cake orders
- PriceOverrideDialog.vb - New custom dialog
- PriceOverride.vb - Helper class

## Version
Implemented in version 1.0.0.19
