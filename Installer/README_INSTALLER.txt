========================================
OVEN DELIGHTS POS - INSTALLER PACKAGE
========================================

SIMPLE INSTALLER CREATION INSTRUCTIONS:
---------------------------------------

Since Inno Setup is having permission issues, use this simple ZIP-based installer instead.

STEP 1: PREPARE THE PACKAGE
---------------------------
1. Create a folder structure like this:
   
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

2. Copy files:
   - Copy ALL files from: C:\Development Apps\Cascades projects\Overn-Delights-POS\Overn-Delights-POS\bin\Release\
   - To: OvenDelightsPOS_Installer\POS\
   
   - Copy font files from: E:\3of9_barcode\ and E:\Free_3_of_9\
   - To: OvenDelightsPOS_Installer\Fonts\

3. Copy Install_POS.bat into the root of OvenDelightsPOS_Installer folder

STEP 2: CREATE THE INSTALLER PACKAGE
------------------------------------
1. Right-click on the OvenDelightsPOS_Installer folder
2. Send to → Compressed (zipped) folder
3. Rename to: OvenDelightsPOS_Setup_v1.0.0.4.zip

STEP 3: UPLOAD TO YOUR WEBSITE
------------------------------
1. Upload OvenDelightsPOS_Setup_v1.0.0.4.zip to your cPanel
2. Put it in: public_html/downloads/ or public_html/pos/
3. Create a download link on your website

USER INSTALLATION INSTRUCTIONS:
-------------------------------
1. Download OvenDelightsPOS_Setup_v1.0.0.4.zip
2. Extract the ZIP file to a temporary location
3. Right-click on Install_POS.bat
4. Select "Run as administrator"
5. Follow the on-screen prompts
6. Installation complete!

WHAT THE INSTALLER DOES:
------------------------
✓ Installs POS to C:\Program Files\Oven Delights POS\
✓ Installs both 3OF9 barcode fonts to Windows Fonts
✓ Creates desktop shortcut
✓ Creates Start Menu shortcut
✓ Auto-update system is enabled and ready
✓ Launches POS after installation

ADVANTAGES OF THIS METHOD:
--------------------------
✓ No Inno Setup permission issues
✓ Simple and reliable
✓ Works on all Windows versions
✓ Easy to update - just replace files and re-zip
✓ Small file size
✓ Professional installation experience

UPDATING THE INSTALLER:
----------------------
When you release a new version:
1. Build new Release version in Visual Studio
2. Update version in Install_POS.bat (line 6)
3. Replace files in POS folder
4. Re-zip the folder
5. Upload new ZIP to website
6. Update version.txt on server for auto-update

========================================
