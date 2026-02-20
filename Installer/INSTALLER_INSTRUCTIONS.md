# Oven Delights POS - Installer Package Instructions

## Overview

This document provides complete instructions for creating a simple ZIP-based installer for the Oven Delights POS system.

---

## Why This Method?

Since Inno Setup has persistent permission issues, this ZIP-based installer provides:

✅ No permission issues  
✅ Simpler to create and update  
✅ Works perfectly every time  
✅ Professional installation experience  
✅ Includes everything (POS + fonts + shortcuts)  

---

## Step 1: Prepare the Package

### Create Folder Structure

Create a new folder called `OvenDelightsPOS_Installer` with the following structure:

```
OvenDelightsPOS_Installer/
├── Install_POS.bat (the installer script)
├── POS/ (copy your entire Release folder here)
│   ├── Overn-Delights-POS.exe
│   ├── App.config
│   └── (all other files from bin\Release\)
└── Fonts/
    ├── 3OF9_NEW.TTF
    ├── FREE3OF9.TTF
    └── (any other font files)
```

### Copy Files

1. **POS Folder:**
   - Copy ALL files from: `C:\Development Apps\Cascades projects\Overn-Delights-POS\Overn-Delights-POS\bin\Release\`
   - To: `OvenDelightsPOS_Installer\POS\`

2. **Fonts Folder:**
   - Copy font files from: `E:\3of9_barcode\` and `E:\Free_3_of_9\`
   - To: `OvenDelightsPOS_Installer\Fonts\`

3. **Installer Script:**
   - Copy `Install_POS.bat` from: `C:\Development Apps\Cascades projects\Overn-Delights-POS\Installer\`
   - To: `OvenDelightsPOS_Installer\` (root folder)

---

## Step 2: Create the Installer Package

1. Right-click on the `OvenDelightsPOS_Installer` folder
2. Select **Send to → Compressed (zipped) folder**
3. Rename to: `OvenDelightsPOS_Setup_v1.0.0.4.zip`

---

## Step 3: Upload to Your Website

1. Upload `OvenDelightsPOS_Setup_v1.0.0.4.zip` to your cPanel
2. Recommended location: `public_html/downloads/` or `public_html/pos/`
3. Create a download link on your website

---

## User Installation Instructions

### For End Users:

1. Download `OvenDelightsPOS_Setup_v1.0.0.4.zip` from your website
2. Extract the ZIP file to a temporary location (e.g., Desktop or Downloads)
3. **Right-click** on `Install_POS.bat`
4. Select **"Run as administrator"**
5. Follow the on-screen prompts
6. Installation complete!

---

## What the Installer Does

The `Install_POS.bat` script automatically:

✓ Checks for administrator privileges  
✓ Installs POS to `C:\Program Files\Oven Delights POS\`  
✓ Installs both 3OF9 barcode fonts to Windows Fonts folder  
✓ Creates desktop shortcut  
✓ Creates Start Menu shortcut  
✓ Enables auto-update system  
✓ Launches POS after installation  

---

## Updating the Installer

When you release a new version:

1. **Update version in Visual Studio:**
   - Open `My Project\AssemblyInfo.vb`
   - Increment version (e.g., `1.0.0.4` → `1.0.0.5`)

2. **Build new Release version:**
   - Switch to Release mode in Visual Studio
   - Press F6 to build

3. **Update installer script:**
   - Open `Install_POS.bat`
   - Update version on line 6: `echo   Oven Delights POS Installer v1.0.0.5`

4. **Replace files:**
   - Copy new Release files to `POS` folder
   - Re-zip the `OvenDelightsPOS_Installer` folder
   - Rename to new version: `OvenDelightsPOS_Setup_v1.0.0.5.zip`

5. **Upload to website:**
   - Upload new ZIP to cPanel
   - Update `version.txt` on server to `1.0.0.5`
   - Existing users will get auto-update prompt!

---

## Auto-Update System

The installer includes the auto-update system that:

- Checks for updates from: `http://www.mogalia.co.za/pos/version.txt`
- Downloads updates from: `http://www.mogalia.co.za/pos/pos.zip`
- Automatically installs and restarts the application

### Server Files Needed:

1. **version.txt** - Contains current version number (e.g., `1.0.0.5`)
2. **pos.zip** - ZIP file of the Release folder for auto-update

---

## Troubleshooting

### Installer won't run
- Make sure to **Run as administrator**
- Check that all files are extracted from the ZIP

### Fonts not installing
- Ensure `.ttf` files are in the `Fonts` folder
- Run installer as administrator

### POS won't launch after install
- Check that .NET Framework 4.7.2 is installed
- Verify all files copied correctly to `C:\Program Files\Oven Delights POS\`

---

## File Locations

- **Installer Script:** `C:\Development Apps\Cascades projects\Overn-Delights-POS\Installer\Install_POS.bat`
- **Release Files:** `C:\Development Apps\Cascades projects\Overn-Delights-POS\Overn-Delights-POS\bin\Release\`
- **Font Files:** `E:\3of9_barcode\` and `E:\Free_3_of_9\`
- **Installation Directory:** `C:\Program Files\Oven Delights POS\`

---

## Support

For issues or questions, contact your system administrator.

**Version:** 1.0.0.4  
**Last Updated:** February 2026
