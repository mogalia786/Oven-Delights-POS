# Oven Delights POS System

**Bismillah** - Modern touch-friendly Point of Sale system for Oven Delights bakery

---

## 🎯 Features

### ✅ Completed
- **Login System** - Integrated with ERP Users table (Username/Password)
- **Idle Screen** - Customer-facing display with promotional messages
- **Main POS Interface** - Touch-optimized layout:
  - **Left Panel (20%)**: Category navigation
  - **Center Panel (50%)**: Product grid with search
  - **Right Panel (30%)**: Shopping cart with totals
  - **Bottom Panel**: Expandable F-key shortcuts (F1-F12)
- **Product Management** - Real-time stock checking
- **Cart Management** - Add, edit quantity, remove items
- **VAT Calculation** - Automatic 15% VAT
- **Keyboard Shortcuts** - Full F1-F12 support

### 🚧 Coming Soon
- Payment processing (Cash, Card, EFT)
- Receipt printing
- Hold/Recall transactions
- Returns processing
- Discounts and promotions
- End of day reports
- Cash drawer integration
- Customer accounts

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
| F1  | New Sale | Clear cart and start new sale |
| F2  | Hold | Hold current sale for later |
| F3  | Search | Focus on search box |
| F4  | Recall | Recall held sale |
| F5  | Qty | Change item quantity |
| F6  | Discount | Apply discount |
| F7  | Remove | Remove selected item |
| F8  | Returns | Process return |
| F9  | Reports | View reports |
| F10 | Cash Drawer | Open cash drawer |
| F11 | Manager | Manager functions |
| F12 | Pay | Process payment |

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

- Payment processing placeholder (coming soon)
- Hold/Recall not yet implemented
- Returns processing pending
- Discount functionality pending

---

## 📝 Notes

- Idle screen appears after 60 seconds of inactivity
- Click anywhere to dismiss idle screen
- All transactions use demo tables by default
- Change `UseDemoTables` to `false` for production

---

**Alhamdulillah** - Built with care for Oven Delights! 🍞
