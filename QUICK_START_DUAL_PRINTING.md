# Quick Start: Dual Printing Setup

## ✅ What You Need to Know

The POS now prints to **TWO printers** automatically:
1. **Thermal Slip Printer** (80mm) - Windows default printer
2. **Continuous Network Printer** - Configured in ERP

## 🚀 Quick Setup (5 Minutes)

### Step 1: Configure in ERP
1. Open **Oven Delights ERP**
2. Click **Utilities** → **Continuous Printer Setup**
3. You'll see the **Receipt Template Designer**

### Step 2: Set Printer Name
1. In the designer, find the **Printer Name** field at the top
2. Enter your network printer path:
   - Example: `\\192.168.1.100\KitchenPrinter`
   - Or: `\\SERVERNAME\PrinterName`
3. Click **Save Configuration**

### Step 3: Test in POS
1. Open POS
2. Complete a sale
3. Check both printers:
   - ✅ Thermal printer should print 80mm slip
   - ✅ Continuous printer should print full receipt

### Step 4: Adjust Layout (Optional)
1. If fields are misaligned on continuous printer:
   - Go back to ERP Receipt Template Designer
   - Drag fields to correct positions
   - Click Save
   - Test again in POS

## 📋 That's It!

No database scripts to run. No code changes needed. Just configure in ERP and it works!

## 🔍 Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| Thermal printer not printing | Check Windows default printer |
| Continuous printer not printing | Check printer name in ERP |
| Fields misaligned | Use ERP designer to adjust positions |
| Printer not found | Verify network path and connectivity |

## 📞 Need Help?

1. Check printer is accessible from Windows
2. Verify configuration in ERP (Utilities → Continuous Printer Setup)
3. Test with a simple sale in POS
4. Adjust positions in ERP designer if needed

---

**That's all you need!** The system uses existing ERP tables, so no additional setup required.
