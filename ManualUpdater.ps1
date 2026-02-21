# Oven Delights POS Manual Updater
# Run this script as Administrator to manually update the POS application

param(
    [string]$InstallPath = "C:\Program Files (x86)\Oven Delights POS",
    [string]$ZipUrl = "http://www.mogalia.co.za/pos/pos.zip"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Oven Delights POS Manual Updater" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator', then run this script again." -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "Step 1: Stopping POS application..." -ForegroundColor Yellow
Get-Process -Name "Overn-Delights-POS" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "Step 2: Downloading update from server..." -ForegroundColor Yellow
$tempZip = "$env:TEMP\pos_update.zip"
$tempExtract = "$env:TEMP\pos_extract"

try {
    # Remove old temp files
    if (Test-Path $tempZip) { Remove-Item $tempZip -Force }
    if (Test-Path $tempExtract) { Remove-Item $tempExtract -Recurse -Force }
    
    # Download
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $ZipUrl -OutFile $tempZip -UseBasicParsing
    Write-Host "   Downloaded successfully!" -ForegroundColor Green
}
catch {
    Write-Host "   ERROR: Failed to download update: $($_.Exception.Message)" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "Step 3: Extracting update files..." -ForegroundColor Yellow
try {
    Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force
    Write-Host "   Extracted successfully!" -ForegroundColor Green
}
catch {
    Write-Host "   ERROR: Failed to extract: $($_.Exception.Message)" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "Step 4: Finding application files..." -ForegroundColor Yellow
# Find the folder containing the exe
$exePath = Get-ChildItem -Path $tempExtract -Filter "Overn-Delights-POS.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $exePath) {
    Write-Host "   ERROR: Could not find Overn-Delights-POS.exe in the update package!" -ForegroundColor Red
    pause
    exit 1
}
$sourceFolder = $exePath.DirectoryName
Write-Host "   Found files in: $sourceFolder" -ForegroundColor Green

Write-Host "Step 5: Backing up current installation..." -ForegroundColor Yellow
$backupPath = "$env:TEMP\POS_Backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
try {
    if (Test-Path $InstallPath) {
        Copy-Item -Path $InstallPath -Destination $backupPath -Recurse -Force
        Write-Host "   Backup created at: $backupPath" -ForegroundColor Green
    }
}
catch {
    Write-Host "   WARNING: Backup failed, but continuing..." -ForegroundColor Yellow
}

Write-Host "Step 6: Installing update files..." -ForegroundColor Yellow
try {
    # Copy all files from source to install path
    Copy-Item -Path "$sourceFolder\*" -Destination $InstallPath -Recurse -Force
    Write-Host "   Files copied successfully!" -ForegroundColor Green
}
catch {
    Write-Host "   ERROR: Failed to copy files: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Attempting to restore backup..." -ForegroundColor Yellow
    if (Test-Path $backupPath) {
        Copy-Item -Path "$backupPath\*" -Destination $InstallPath -Recurse -Force
        Write-Host "   Backup restored!" -ForegroundColor Green
    }
    pause
    exit 1
}

Write-Host "Step 7: Cleaning up temporary files..." -ForegroundColor Yellow
Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "UPDATE COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Starting Oven Delights POS..." -ForegroundColor Cyan

Start-Sleep -Seconds 2
Start-Process -FilePath "$InstallPath\Overn-Delights-POS.exe"

Write-Host ""
Write-Host "Update complete! The application has been started." -ForegroundColor Green
pause
