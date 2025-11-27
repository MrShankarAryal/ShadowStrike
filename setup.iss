; ShadowStrike Installer Script for Inno Setup
; Download Inno Setup from: https://jrsoftware.org/isdl.php

[Setup]
; Basic Information
AppName=ShadowStrike
AppVersion=2.0
AppPublisher=Shankar Aryal
AppPublisherURL=https://github.com/MrShankarAryal/ShadowStrike
AppSupportURL=https://github.com/MrShankarAryal/ShadowStrike/issues
AppUpdatesURL=https://github.com/MrShankarAryal/ShadowStrike/releases
DefaultDirName={autopf}\ShadowStrike
DefaultGroupName=ShadowStrike
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=installer
OutputBaseFilename=ShadowStrike-Setup-v2.0
SetupIconFile=ShadowStrike.UI\img\eagle.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin


; Visual Customization
WizardImageFile=compiler:WizModernImage-IS.bmp
WizardSmallImageFile=compiler:WizModernSmallImage-IS.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
Source: "publish\ShadowStrike.UI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ShadowStrike"; Filename: "{app}\ShadowStrike.UI.exe"; IconFilename: "{app}\ShadowStrike.UI.exe"
Name: "{group}\{cm:UninstallProgram,ShadowStrike}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ShadowStrike"; Filename: "{app}\ShadowStrike.UI.exe"; Tasks: desktopicon; IconFilename: "{app}\ShadowStrike.UI.exe"
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\ShadowStrike"; Filename: "{app}\ShadowStrike.UI.exe"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\ShadowStrike.UI.exe"; Description: "{cm:LaunchProgram,ShadowStrike}"; Flags: nowait postinstall skipifsilent

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nShadowStrike is an advanced security testing and penetration testing platform built with .NET 8.%n%n⚠️ WARNING: This tool is for authorized security testing only. Unauthorized use is illegal.%n%nIt is recommended that you close all other applications before continuing.
FinishedLabel=Setup has finished installing [name] on your computer.%n%nYou can now launch ShadowStrike from the Start Menu or Desktop shortcut.

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  if MsgBox('ShadowStrike is a security testing tool for authorized use only.' + #13#10 + #13#10 + 
            'By installing this software, you agree to use it only on systems you own or have explicit permission to test.' + #13#10 + #13#10 + 
            'Unauthorized use may violate local, state, or federal laws.' + #13#10 + #13#10 + 
            'Do you accept these terms and wish to continue?', 
            mbConfirmation, MB_YESNO) = IDNO then
    Result := False;
end;
