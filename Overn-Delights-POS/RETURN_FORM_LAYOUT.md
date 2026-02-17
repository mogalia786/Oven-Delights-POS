# Return Form Layout - Updated Design

## Form Dimensions
- **Width:** 1100px (increased from 1000px)
- **Height:** 800px

## Layout Structure

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                        🔄 RETURN - INV-PH-TILL01-000123                        │
│                              (Header - Dark Blue)                              │
└────────────────────────────────────────────────────────────────────────────────┘

SELECT ITEMS TO RETURN:

┌────────────────────────────────────────────────────────────────────────────────┐
│ ITEM DETAILS                                    RESTOCK?         ACTIONS       │
│                                                                                │
├────────────────────────────────────────────────────────────────────────────────┤
│ CAKE001 - Chocolate Cake | Qty: 2 | R 50.00    ☑         [🔄 RETURN] [➖ MINUS]│
│                                                                                │
├────────────────────────────────────────────────────────────────────────────────┤
│ BREAD002 - White Bread | Qty: 5 | R 15.00      ☑         [🔄 RETURN] [➖ MINUS]│
│                                                                                │
├────────────────────────────────────────────────────────────────────────────────┤
│ COKE003 - Coca Cola 2L | Qty: 3 | R 25.00      ☐         [🔄 RETURN] [➖ MINUS]│
│                                                                                │
└────────────────────────────────────────────────────────────────────────────────┘

CUSTOMER DETAILS:
Customer Name: [________________]    Phone Number: [________________]
Address: [_____________________________________________________________]

Reason for Return:
[___________________________________________________________________________]
[___________________________________________________________________________]

┌────────────────────────────────────────────────────────────────────────────────┐
│                                                                                │
│  R 175.00                                      [CANCEL]        [PROCESS RETURN]│
│  (Total Return Amount)                                                         │
└────────────────────────────────────────────────────────────────────────────────┘
```

## Column Alignment

### Header Row (Dark Blue Background)
| Column | Position | Content |
|--------|----------|---------|
| 1 | X: 10 | "ITEM DETAILS" |
| 2 | X: 680 | "RESTOCK?" |
| 3 | X: 850 | "ACTIONS" |

### Line Item Row (White Background)
| Element | Position | Width | Description |
|---------|----------|-------|-------------|
| Item Label | X: 10, Y: 18 | 650px | Product code, name, qty, price, total |
| Checkbox | X: 695, Y: 20 | 20px | Restock checkbox (no text) |
| Return Button | X: 780, Y: 10 | 120px | Green "🔄 RETURN" button |
| Minus Button | X: 910, Y: 10 | 120px | Orange "➖ MINUS" button |

## Key Changes

### 1. Form Width
- **Before:** 1000px
- **After:** 1100px
- **Reason:** More space for all elements

### 2. Line Item Panel
- **Before:** 900px width
- **After:** 1020px width
- **Reason:** Accommodate wider layout

### 3. Column Headers
- **New:** Added header panel with column titles
- **Background:** Dark blue matching form header
- **Labels:** "ITEM DETAILS", "RESTOCK?", "ACTIONS"

### 4. Checkbox
- **Before:** Text "Put Back Into Stock" (hidden by item description)
- **After:** No text, just checkbox aligned under "RESTOCK?" header
- **Position:** X: 695 (centered under header)
- **Size:** 20x20px

### 5. Button Positions
- **Return Button:** Moved from X: 650 to X: 780
- **Minus Button:** Moved from X: 780 to X: 910
- **Reason:** Make room for checkbox column

## Visual Spacing

```
[Item Description: 650px]  [Gap: 25px]  [☑: 20px]  [Gap: 65px]  [Return: 120px]  [Gap: 10px]  [Minus: 120px]
|<------------------>|      |<------>|   |<--->|   |<------>|   |<---------->|   |<------>|   |<---------->|
0                   650     675      695  715   735          780             900  910      1030
```

## User Experience

### Clear Visual Hierarchy
1. **Header row** clearly labels each column
2. **Checkbox** is obvious and aligned
3. **Buttons** are clearly in the "ACTIONS" column

### Checkbox Behavior
- ✅ **Checked (default):** Item will be put back into stock
- ☐ **Unchecked:** Item will be discarded (written off)

### Tooltip (Future Enhancement)
Could add tooltip on checkbox hover:
- "Check to put item back into stock"
- "Uncheck if item is damaged/expired"

## Responsive Design Notes

- Form is fixed width (not resizable)
- FlowLayoutPanel has AutoScroll for many items
- All elements use absolute positioning for precise alignment
- Column headers ensure alignment is maintained

## Color Scheme

| Element | Color | Hex Code |
|---------|-------|----------|
| Header Background | Dark Blue | #2C3E50 |
| Return Button | Green | #27AE60 |
| Minus Button | Orange | #E67E22 |
| Total Amount | Yellow | #F39C12 |
| Panel Background | White | #FFFFFF |

## Accessibility

- Large, clear buttons (120x40px)
- High contrast colors
- Clear labels and headers
- Checkbox is standard size (20x20px)
- Font sizes: 10-14pt for readability
