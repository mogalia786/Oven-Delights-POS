; Oven Delights POS Installer Script
; Created with Inno Setup

#define MyAppName "Oven Delights POS"
#define MyAppVersion "1.0.0.5"
#define MyAppPublisher "Oven Delights"
#define MyAppURL "http://www.mogalia.co.za"
#define MyAppExeName "Overn-Delights-POS.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{A8B9C1D2-E3F4-5678-9ABC-DEF012345678}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Oven Delights POS
DefaultGroupName=Oven Delights POS
AllowNoIcons=yes
LicenseFile=
OutputDir=C:\Output
OutputBaseFilename=OvenDelightsPOS_Setup_v{#MyAppVersion}
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application files from Release folder
Source: "C:\Development Apps\Cascades projects\Overn-Delights-POS\Overn-Delights-POS\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; 3OF9 Font files - specify exact filenames
Source: "E:\3of9_barcode\3OF9_NEW.TTF"; DestDir: "{fonts}"; Flags: onlyifdoesntexist uninsneveruninstall fontisnttruetype
Source: "E:\Free_3_of_9\FREE3OF9.TTF"; DestDir: "{fonts}"; Flags: onlyifdoesntexist uninsneveruninstall fontisnttruetype

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNetInstalled(): Boolean;
var
  Success: Boolean;
  InstallSuccess: Boolean;
  ResultCode: Integer;
begin
  // Check if .NET Framework 4.7.2 or higher is installed
  Success := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full');
  Result := Success;
  
  if not Result then
  begin
    if MsgBox('.NET Framework 4.7.2 or higher is required but not installed.' + #13#10 + #13#10 +
              'Would you like to download and install it now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet-framework/net472', '', '', SW_SHOW, ewNoWait, ResultCode);
      MsgBox('Please install .NET Framework 4.7.2 and then run this installer again.', mbInformation, MB_OK);
      Result := False;
    end
    else
    begin
      MsgBox('Installation cannot continue without .NET Framework 4.7.2.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := IsDotNetInstalled();
end;
