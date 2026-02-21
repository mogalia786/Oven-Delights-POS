@echo off
echo ========================================
echo Oven Delights POS Quick Updater
echo ========================================
echo.

REM Check for admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click this file and select "Run as administrator"
    pause
    exit /b 1
)

echo Stopping POS application...
taskkill /F /IM "Overn-Delights-POS.exe" >nul 2>&1
timeout /t 3 /nobreak >nul

echo Downloading update from server...
powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'http://www.mogalia.co.za/pos/pos.zip' -OutFile '%TEMP%\pos_update.zip'"

if not exist "%TEMP%\pos_update.zip" (
    echo ERROR: Failed to download update!
    pause
    exit /b 1
)

echo Extracting update...
powershell -Command "Expand-Archive -Path '%TEMP%\pos_update.zip' -DestinationPath '%TEMP%\pos_extract' -Force"

echo Finding application files...
set "SOURCE_DIR=%TEMP%\pos_extract"
if exist "%TEMP%\pos_extract\Overn-Delights-POS.exe" set "SOURCE_DIR=%TEMP%\pos_extract"
if exist "%TEMP%\pos_extract\POS\Overn-Delights-POS.exe" set "SOURCE_DIR=%TEMP%\pos_extract\POS"
if exist "%TEMP%\pos_extract\Overn-Delights-POS\Overn-Delights-POS.exe" set "SOURCE_DIR=%TEMP%\pos_extract\Overn-Delights-POS"

echo Copying files to installation directory...
robocopy "%SOURCE_DIR%" "C:\Program Files (x86)\Oven Delights POS" /E /IS /IT /R:3 /W:1 /NFL /NDL /NJH /NJS

if %errorLevel% leq 7 (
    echo Files copied successfully!
) else (
    echo WARNING: Some files may not have been copied (exit code: %errorLevel%^)
)

echo Cleaning up temporary files...
del "%TEMP%\pos_update.zip" >nul 2>&1
rmdir /s /q "%TEMP%\pos_extract" >nul 2>&1

echo.
echo ========================================
echo UPDATE COMPLETE!
echo ========================================
echo.
echo Starting Oven Delights POS...
timeout /t 2 /nobreak >nul
start "" "C:\Program Files (x86)\Oven Delights POS\Overn-Delights-POS.exe"

echo.
echo Update complete! The application has been started.
pause
