# F-KEY LAYOUT - UPDATED

## Complete F-Key Mapping

### **F1 - 🆕 New Sale**
- Clears current cart
- Shows idle screen
- Hides keyboard if visible
- Resets search box
- **Usage**: Start fresh sale

### **F2 - ⏸️ Hold Sale**
- Saves current cart to database
- Generates hold number
- Clears cart
- **Usage**: Customer needs to step away

### **F3 - 🔍 Search by Code**
- Focuses search textbox
- User types with **physical keyboard**
- Searches by **SKU/ItemCode** (barcode)
- Uses database query
- **Usage**: Quick barcode/code search

### **F4 - ⌨️ Search by Name**
- Shows/hides on-screen QWERTY keyboard
- User types with **on-screen keyboard**
- Searches by **ProductName**
- Uses cached products (faster)
- Wildcard search (%text%)
- **Usage**: Touch-screen product name search

### **F5 - 📋 Recall Sale**
- Shows list of held sales
- Select to recall
- Loads items back to cart
- **Usage**: Resume held sale

### **F6 - 🔎 Product Lookup**
- Opens product lookup form
- Predictive search with numpad
- Shows stock levels
- **Usage**: Find product when not in current category

### **F7 - 💰 Discount %**
- Apply percentage discount to selected item
- **Requires supervisor authorization**
- Shows discount dialog
- Live price preview
- **Usage**: Apply manager discount

### **F8 - ❌ Remove Item**
- Remove selected line item
- **Requires supervisor authorization**
- Confirmation dialog
- **Usage**: Remove item from cart

### **F9 - ↩️ Returns**
- Process product returns
- **Requires supervisor authorization**
- Invoice number entry
- Line item selection
- **Usage**: Process customer returns

### **F10 - 🎂 Cake Orders**
- Custom cake order system
- Quotation generation
- Deposit payments
- Manufacturing integration
- **Usage**: Special cake orders

### **F11 - 👔 Manager Functions**
- Manager menu access
- Reports
- Cash up
- **Usage**: Manager operations

### **F12 - 💳 PAY**
- Process payment
- Multiple payment methods
- Receipt printing
- **Usage**: Complete sale

---

## Search Methods Comparison

| Feature | F3 - Code Search | F4 - Name Search |
|---------|------------------|------------------|
| **Input** | Physical keyboard | On-screen keyboard |
| **Searches** | SKU/ItemCode | ProductName |
| **Method** | Database query | Cached filter |
| **Pattern** | `LIKE '%code%'` | `%name%` wildcard |
| **Speed** | Database query | Instant (cached) |
| **Use Case** | Barcode scanning | Touch-screen browsing |
| **Best For** | Known codes | Browsing by name |

---

## Keyboard Shortcuts Summary

```
┌─────────────────────────────────────────────────────────────┐
│  F1      F2      F3       F4       F5       F6       F7     │
│  New    Hold    Code     Name   Recall   Lookup   Disc%    │
│                                                              │
│  F8      F9      F10      F11      F12                      │
│ Remove  Return   Cake   Manager   PAY                       │
└─────────────────────────────────────────────────────────────┘
```

---

## Workflow Examples

### **Example 1: Barcode Search (F3)**
1. Press **F3**
2. Search box gets focus
3. Scan barcode or type code with physical keyboard
4. Products matching code appear
5. Click product to add to cart

### **Example 2: Name Search (F4)**
1. Press **F4**
2. On-screen keyboard slides up
3. Click letters to type product name
4. Products filter in real-time
5. Click product to add to cart
6. Press **F4** again to hide keyboard

### **Example 3: Hold & Recall**
1. Customer scanning items
2. Customer forgot wallet
3. Press **F2** (Hold) - Sale saved
4. Serve next customer
5. First customer returns
6. Press **F5** (Recall)
7. Select held sale
8. Continue with payment

---

## Authorization Requirements

**Functions requiring Retail Supervisor authorization:**
- ✅ **F7** - Discount
- ✅ **F8** - Remove Item
- ✅ **F9** - Returns

**Functions without authorization:**
- F1 - New Sale
- F2 - Hold Sale
- F3 - Search by Code
- F4 - Search by Name
- F5 - Recall Sale
- F6 - Product Lookup
- F10 - Cake Orders
- F11 - Manager Functions
- F12 - Payment

---

## Changes from Previous Version

### **Removed:**
- ~~F10 - Cash Drawer~~ (moved to manager functions)

### **Added:**
- **F4 - Search by Name** (on-screen keyboard)

### **Reorganized:**
- F3 → Search by Code (kept original functionality)
- F4 → Search by Name (new on-screen keyboard)
- F5 → Recall (was F4)
- F6 → Product Lookup (was F5)
- F7 → Discount (was F6)
- F8 → Remove (was F7)
- F9 → Returns (was F8)
- F10 → Cake Orders (was F9)
- F11 → Manager (was F11, unchanged)
- F12 → PAY (was F12, unchanged)

---

**Status: UPDATED** ✅

All F-keys now properly mapped with clear separation between code search (F3) and name search (F4)!
