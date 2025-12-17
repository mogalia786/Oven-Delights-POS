# UNDERSTANDING THE PRICE ISSUE

## THE PROBLEM

You said "everywhere else the product price is 44" but the database shows 50.00 for product 16009.

This suggests one of two scenarios:

### SCENARIO 1: Database has WRONG prices (VAT-exclusive instead of VAT-inclusive)
- Demo_Retail_Price stores prices as VAT-exclusive (e.g., R43.48)
- System expects VAT-inclusive prices (e.g., R50.00)
- When you see R44.00 "everywhere else", it's because those systems are correctly adding VAT
- POS is reading R50.00 from database and treating it as VAT-inclusive

### SCENARIO 2: Database has MIXED prices (some correct, some wrong)
- Some products have correct VAT-inclusive prices (R44.00)
- Some products have incorrect prices (R50.00)
- Need to identify which products are wrong and fix them

## WHAT WE NEED TO KNOW

**Question 1**: When you say "everywhere else the product price is 44", where are you seeing R44.00?
- Product card in POS? (This comes from Demo_Retail_Price)
- ERP system?
- Price list/spreadsheet?
- Category navigation?

**Question 2**: Are ALL products showing wrong prices in cart, or just some?

**Question 3**: What is the CORRECT price for product 16009 (BC Vanilla Swiss Roll)?
- Is it R44.00 (VAT-inclusive)?
- Or is it R50.00 (VAT-inclusive)?

## NEXT STEPS

1. Run DIAGNOSE_PRICE_ISSUE_ALL_PRODUCTS.sql to see pattern across multiple products
2. Based on results, determine if:
   - ALL prices need to be recalculated (bulk fix)
   - SOME prices are wrong (selective fix)
   - Prices are stored correctly but code is wrong (no DB fix needed)

## IMPORTANT

Do NOT run any price update scripts until we understand the root cause!
