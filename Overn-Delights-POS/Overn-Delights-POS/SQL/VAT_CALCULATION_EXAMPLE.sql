-- VAT Calculation Example - Working BACKWARD from VAT-Inclusive Prices
-- =====================================================================

-- SCENARIO: Cart has items with VAT-INCLUSIVE prices
-- Example: 2 items @ R115.00 each (VAT already included)

DECLARE @CartTotal DECIMAL(18,2) = 230.00  -- Total from cart (VAT-INCLUSIVE)

-- WRONG WAY (what we were doing before):
-- ❌ Subtotal = R230.00
-- ❌ VAT = R230.00 × 0.15 = R34.50
-- ❌ Total = R230.00 + R34.50 = R264.50  <-- WRONG! Double-taxed!

PRINT '❌ WRONG CALCULATION (Adding VAT to VAT-inclusive price):'
PRINT 'Cart Total (incl VAT): R' + CAST(@CartTotal AS VARCHAR(20))
PRINT 'VAT Added (15%): R' + CAST(@CartTotal * 0.15 AS VARCHAR(20))
PRINT 'Final Total: R' + CAST(@CartTotal + (@CartTotal * 0.15) AS VARCHAR(20))
PRINT ''
PRINT '================================================'
PRINT ''

-- CORRECT WAY (working backward):
-- ✅ Total (incl VAT) = R230.00
-- ✅ Subtotal (excl VAT) = R230.00 / 1.15 = R200.00
-- ✅ VAT = R230.00 - R200.00 = R30.00
-- ✅ Total = R230.00 (correct!)

DECLARE @SubtotalExcl DECIMAL(18,2) = ROUND(@CartTotal / 1.15, 2)
DECLARE @VAT DECIMAL(18,2) = ROUND(@CartTotal - @SubtotalExcl, 2)

PRINT '✅ CORRECT CALCULATION (Working backward from VAT-inclusive):'
PRINT 'Cart Total (incl VAT): R' + CAST(@CartTotal AS VARCHAR(20))
PRINT 'Subtotal (excl VAT): R' + CAST(@SubtotalExcl AS VARCHAR(20))
PRINT 'VAT (15%): R' + CAST(@VAT AS VARCHAR(20))
PRINT 'Final Total: R' + CAST(@CartTotal AS VARCHAR(20))
PRINT ''
PRINT 'Verification: R' + CAST(@SubtotalExcl AS VARCHAR(20)) + ' × 1.15 = R' + CAST(ROUND(@SubtotalExcl * 1.15, 2) AS VARCHAR(20))
PRINT ''
PRINT '================================================'
PRINT ''

-- BREAKDOWN on Receipt:
PRINT 'Receipt Display:'
PRINT '  Subtotal (excl VAT): R' + CAST(@SubtotalExcl AS VARCHAR(20))
PRINT '  VAT (15%):           R' + CAST(@VAT AS VARCHAR(20))
PRINT '  ----------------------------------------'
PRINT '  TOTAL:               R' + CAST(@CartTotal AS VARCHAR(20))
GO
