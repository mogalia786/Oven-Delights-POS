# Oven Delights POS System

**Bismillah** - Modern touch-friendly Point of Sale system for Oven Delights bakery

---

## 🎯 Features

### ✅ Completed
- **Login System** - Integrated with ERP Users table (Username/Password)
  - Branch selection for Super Administrators
  - Till point setup and configuration
- **Idle Screen** - Customer-facing display with promotional messages
  - Auto-activates after 60 seconds of inactivity
  - Rotating promotional messages
- **Main POS Interface** - Touch-optimized redesigned layout:
  - **Top Bar**: Branch info, cashier name, cash up, logout
  - **Breadcrumb Navigation**: Category > Subcategory > Products
  - **Product Area**: Large touch-friendly tiles with images
  - **Cart Panel**: Right-side cart with totals and payment
  - **F-Key Shortcuts**: Bottom bar with all functions
- **Product Management** 
  - Real-time stock checking from RetailStock table
  - Category and subcategory navigation
  - Product search by code and name
  - Barcode scanner support
- **Cart Management** 
  - Add, edit quantity, remove items
  - Touch-friendly quantity adjustment
  - Line item totals
- **Payment Processing** - Multi-tender support:
  - 💵 Cash payments with change calculation
  - 💳 Credit Card (Speedpoint integration ready)
  - 🏦 EFT payments
  - ✋ Manual card entry
  - 🔀 Split payments (cash + card)
- **Receipt Printing**
  - Till slip preview and print
  - Continuous Epson dot matrix printer support
  - Template-driven printing from ERP ReceiptTemplateConfig
  - Dual printing (modal dialog + continuous printer)
- **Order Management**
  - F11: Create custom orders (cakes, special items)
  - Customer details capture with on-screen keyboard
  - Order ready date/time selection
  - Colour and picture specifications
  - Special instructions
  - Deposit collection
  - F12: Order collection and balance payment
  - Order receipts with full details
- **Returns Processing** (F9)
  - Invoice number lookup
  - Line item selection for return
  - Stock restoration
  - Supervisor authorization required
  - Return receipt printing
- **Void Sale** (F10)
  - Clear entire cart
  - Supervisor authorization required
- **Cash Up** (💰 Button)
  - End of shift cash reconciliation
  - Sales summary with transaction counts
  - Payment breakdown (cash/card for sales and orders)
  - Return tracking
  - Order tracking (cake orders and general orders)
  - Cash float management
  - Total cash in till calculation
  - Printable cash up report
  - Supervisor authorization required
- **VAT Calculation** - Automatic 15% VAT (inclusive pricing)
- **Keyboard Shortcuts** - Full F1-F12 support
- **Responsive Design** - Adapts to different screen sizes
- **GL Integration** - Automatic posting to General Ledger and Journals

### 🚧 Coming Soon
- Customer accounts and debtor integration
- Layby management
- Advanced discounts and promotions
- Loyalty program integration
- Multi-language support

---

## 🗂️ Project Structure

```
Overn-Delights-POS/
├── Services/
│   └── POSDataService.vb          # Data layer with demo table switching
├── Forms/
│   ├── LoginForm.vb               # ERP authentication
│   ├── IdleScreen.vb              # Customer-facing display
│   └── POSMainForm.vb             # Main POS interface
├── ApplicationEvents.vb           # Startup flow
└── App.config                     # Configuration
```

---

## ⚙️ Configuration

### App.config Settings

```xml
<connectionStrings>
  <add name="OvenDelightsERPConnectionString" 
       connectionString="Your Azure SQL connection string" />
</connectionStrings>

<appSettings>
  <!-- Use Demo tables for development -->
  <add key="UseDemoTables" value="true" />
  
  <!-- POS Settings -->
  <add key="CompanyName" value="Oven Delights" />
  <add key="VATRate" value="0.15" />
  
  <!-- UI Colors -->
  <add key="PrimaryColor" value="#D2691E" />
  <add key="SecondaryColor" value="#8B4513" />
  <add key="AccentColor" value="#FFD700" />
</appSettings>
```

---

## 🎨 Design Layout

### Main POS Screen

```
┌─────────────────────────────────────────────────────────────┐
│  OVEN DELIGHTS                    Cashier: John    [Logout] │
├──────────┬────────────────────────────────┬─────────────────┤
│          │                                │  CURRENT SALE   │
│ CATEGORY │  [Search Products...]          │                 │
│          │                                │  ┌────────────┐ │
│ Bread    │  ┌──────┬──────┬──────┬─────┐ │  │ Cart Items │ │
│ Pastries │  │ SKU  │ Name │ Price│Stock│ │  │            │ │
│ Cakes    │  ├──────┼──────┼──────┼─────┤ │  │            │ │
│ Cookies  │  │ B001 │Bread │ R25  │ 50  │ │  │            │ │
│ Pies     │  │ B002 │Roll  │ R15  │ 30  │ │  └────────────┘ │
│          │  └──────┴──────┴──────┴─────┘ │                 │
│          │                                │  Subtotal: R0   │
│          │                                │  VAT:      R0   │
│          │                                │  TOTAL:    R0   │
│          │                                │  [   PAY   ]    │
├──────────┴────────────────────────────────┴─────────────────┤
│ [F1 New] [F2 Hold] [F3 Search] [F4 Recall] ... [▼ More]    │
└─────────────────────────────────────────────────────────────┘
```

---

## ⌨️ Keyboard Shortcuts

| Key | Function | Description |
|-----|----------|-------------|
| F1  | 🆕 New Sale | Clear cart and start new sale |
| F2  | ⏸️ Hold | Hold current sale for later |
| F3  | 🔍 Search | Focus on search box |
| F4  | 📋 Recall | Recall held sale |
| F5  | 🔢 Qty | Change item quantity |
| F6  | 💰 Discount | Apply discount (coming soon) |
| F7  | ❌ Remove | Remove selected item from cart |
| F8  | 📦 Stock | Stock lookup (coming soon) |
| F9  | ↩️ Return | Process product return |
| F10 | ❌ Void | Void entire sale (supervisor auth) |
| F11 | 📝 Order | Create custom order (cakes, etc.) |
| F12 | 📦 Collect | Collect pre-paid order |

---

## 🔐 Authentication

### User Roles Allowed
- Cashier
- Branch Manager
- Super Administrator

### Login Process
1. Enter username and password
2. System validates against ERP Users table
3. Checks role permissions
4. Loads user's branch data
5. Opens main POS interface

---

## 💾 Database Integration

### Demo Mode (Development)
- Uses `Demo_` prefixed tables
- Safe testing without affecting production
- Simulated prices and stock

### Production Mode (Live)
- Uses production `Retail_` tables
- Real stock and pricing
- Full transaction recording

**Switch modes in App.config:**
```xml
<add key="UseDemoTables" value="false" />
```

---

## 🚀 Getting Started

### Prerequisites
1. Visual Studio 2019 or later
2. .NET Framework 4.7.2
3. Access to Oven Delights ERP database
4. Demo tables populated (see ERP Database/POS_Demo/)

### Setup
1. Open solution in Visual Studio
2. Update connection string in App.config
3. Ensure demo tables are populated
4. Build solution (F6)
5. Run (F5)

### First Run
1. Login with ERP credentials
2. System loads products from demo tables
3. Browse categories and products
4. Add items to cart
5. Test all F-key shortcuts

---

## 📊 Data Flow

```
Login → Validate User → Load Branch Data
  ↓
Main POS Screen
  ↓
Select Products → Add to Cart → Calculate Totals
  ↓
Process Payment → Update Stock → Print Receipt
  ↓
Record Transaction → Update Ledgers
```

---

## 🎯 Next Development Steps

1. **Payment Form** - Cash, Card, EFT processing
2. **Receipt Printing** - Thermal printer integration
3. **Hold/Recall** - Save and retrieve transactions
4. **Returns** - Process product returns
5. **Reports** - Sales, stock, cashier reports
6. **Hardware** - Cash drawer, barcode scanner
7. **Customer Accounts** - Debtor integration

---

## 🐛 Known Issues

- Discount functionality (F6) not yet implemented
- Stock lookup (F8) not yet implemented
- Customer account integration pending
- Layby functionality pending

---

## 📝 Notes

### UI Behavior
- Idle screen appears after 60 seconds of inactivity
- Click anywhere to dismiss idle screen and return to categories
- After completing a sale, system returns to categories screen
- All payment forms are large, centered, non-fullscreen dialogs

### Recent Updates (Nov 2024)
- ✅ Numpad display fixed - all buttons (7,8,9) now visible
- ✅ First digit override - typing replaces total instead of appending
- ✅ Idle timer extended to 60 seconds
- ✅ Order receipts now show Colour and Picture fields
- ✅ Cash up report includes order cash and card amounts
- ✅ Total Cash in Till now includes order payments

### Database
- Production tables: `Retail_Product`, `Retail_Price`, `RetailStock`
- Demo tables: `Demo_Retail_Product`, `Demo_Retail_Price` (for testing)
- Orders: `POS_CustomOrders`, `POS_CustomOrderItems`
- Sales: `Demo_Sales`, `Demo_SaleItems`
- Returns: `Demo_Returns`, `Demo_ReturnItems`
- GL: `Ledgers`, `Journals`

### Printing
- Till slip: Modal preview dialog
- Continuous printer: Epson dot matrix using ERP templates
- Both printers work simultaneously
- Template coordinates from `ReceiptTemplateConfig` table

---

**Alhamdulillah** - Built with care for Oven Delights! 🍞
