AUTOMATIC FONT INSTALLATION FOR POS DEPLOYMENT
==============================================

The POS application requires the "Free 3 of 9" barcode font to generate scannable barcodes.

SETUP INSTRUCTIONS:
-------------------

1. Download the font file:
   - Go to: https://www.dafont.com/3of9-barcode.font
   - Download and extract 3OF9_NEW.TTF

2. Copy font file to Setup folder:
   - Place 3OF9_NEW.TTF in the same folder as InstallFont.vbs
   - This folder: C:\Development Apps\Cascades projects\Overn-Delights-POS\Overn-Delights-POS\Setup\

3. Include in ClickOnce publish:
   - In Visual Studio, right-click project → Properties → Publish
   - Click "Application Files" button
   - Add 3OF9_NEW.TTF and InstallFont.vbs to the list
   - Set both to "Include" and "Data File"

4. Run installer script on each POS machine:
   - After installing the application, run InstallFont.vbs as Administrator
   - Right-click InstallFont.vbs → Run as Administrator
   - This will install the font system-wide

ALTERNATIVE: Manual Installation
---------------------------------
If automatic installation doesn't work:
1. Copy 3OF9_NEW.TTF to each POS machine
2. Right-click the font file → Install
3. Restart the POS application

FALLBACK:
---------
If the font is not installed, the application will use manual Code 39 barcode rendering.
This should still work but may not be as reliable as the font-based rendering.
