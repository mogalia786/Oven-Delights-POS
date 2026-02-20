@echo off
title Oven Delights POS Installer
color 0A

echo.
echo ========================================
echo   Oven Delights POS Installer v1.0.0.4
echo ========================================
echo.

:: Check for admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This installer requires Administrator privileges.
    echo Please right-click and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

:: Set installation directory
set "INSTALL_DIR=C:\Program Files\Oven Delights POS"

echo Installing to: %INSTALL_DIR%
echo.

:: Create installation directory
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Copy all files from current directory to installation directory
echo Copying application files...
xcopy /E /I /Y /Q "POS\*" "%INSTALL_DIR%\" >nul

:: Install fonts
echo Installing barcode fonts...
if exist "Fonts\*.ttf" (
    copy /Y "Fonts\*.ttf" "%WINDIR%\Fonts\" >nul
    reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts" /v "3 of 9 Barcode (TrueType)" /t REG_SZ /d "3OF9_NEW.TTF" /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts" /v "Free 3 of 9 (TrueType)" /t REG_SZ /d "FREE3OF9.TTF" /f >nul 2>&1
)

:: Create desktop shortcut
echo Creating desktop shortcut...
powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%PUBLIC%\Desktop\Oven Delights POS.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\Overn-Delights-POS.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.Save()"

:: Create Start Menu shortcut
echo Creating Start Menu shortcut...
if not exist "%ProgramData%\Microsoft\Windows\Start Menu\Programs\Oven Delights POS\" mkdir "%ProgramData%\Microsoft\Windows\Start Menu\Programs\Oven Delights POS\"
powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%ProgramData%\Microsoft\Windows\Start Menu\Programs\Oven Delights POS\Oven Delights POS.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\Overn-Delights-POS.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.Save()"

echo.
echo ========================================
echo   Installation Complete!
echo ========================================
echo.
echo Oven Delights POS has been installed to:
echo %INSTALL_DIR%
echo.
echo Desktop shortcut created.
echo Start Menu shortcut created.
echo.
echo Press any key to launch Oven Delights POS...
pause >nul

start "" "%INSTALL_DIR%\Overn-Delights-POS.exe"
exit
