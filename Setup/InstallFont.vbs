' Install Free 3 of 9 Barcode Font
' This script runs during application installation

Option Explicit

Dim objShell, objFSO, fontFile, fontsFolder, fontName

Set objShell = CreateObject("Shell.Application")
Set objFSO = CreateObject("Scripting.FileSystemObject")

' Font file should be in same directory as this script
fontFile = objFSO.GetParentFolderName(WScript.ScriptFullName) & "\3OF9_NEW.TTF"
fontsFolder = CreateObject("WScript.Shell").SpecialFolders("Fonts")
fontName = "Free 3 of 9 (TrueType)"

' Check if font file exists
If objFSO.FileExists(fontFile) Then
    ' Check if font is already installed
    If Not objFSO.FileExists(fontsFolder & "\3OF9_NEW.TTF") Then
        ' Install the font
        objShell.Namespace(fontsFolder).CopyHere fontFile
        
        ' Register font in registry
        Dim objRegistry
        Set objRegistry = CreateObject("WScript.Shell")
        objRegistry.RegWrite "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts\" & fontName, "3OF9_NEW.TTF", "REG_SZ"
        
        WScript.Echo "Font installed successfully"
    Else
        WScript.Echo "Font already installed"
    End If
Else
    WScript.Echo "Font file not found: " & fontFile
End If

Set objShell = Nothing
Set objFSO = Nothing
