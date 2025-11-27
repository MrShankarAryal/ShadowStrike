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


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
Source: "publish\ShadowStrike.UI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "ShadowStrike.UI\img\eagle.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ShadowStrike"; Filename: "{app}\ShadowStrike.UI.exe"; IconFilename: "{app}\eagle.ico"
Name: "{group}\{cm:UninstallProgram,ShadowStrike}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ShadowStrike"; Filename: "{app}\ShadowStrike.UI.exe"; Tasks: desktopicon; IconFilename: "{app}\eagle.ico"
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\ShadowStrike"; Filename: "{app}\ShadowStrike.UI.exe"; Tasks: quicklaunchicon; IconFilename: "{app}\eagle.ico"

[Run]
Filename: "{app}\ShadowStrike.UI.exe"; Description: "{cm:LaunchProgram,ShadowStrike}"; Flags: nowait postinstall skipifsilent

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nShadowStrike is an advanced security testing and penetration testing platform built with .NET 8 and WPF.%n%n** FEATURES **%n  * OSINT Engine - Comprehensive target analysis and reconnaissance%n  * DDoS Modules - HTTP Flood, SYN Flood, UDP Flood with real-time monitoring%n  * Injection Testing - SQL injection scanner and file upload vulnerability tester%n  * Ransomware Analysis - Security testing and detection capabilities%n  * Terminal Interface - Integrated command-line access%n  * Logs and History - Automatic JSON logging with historical scan management%n  * Modern UI - Material Design interface with professional menu bar%n%n** WARNING ** This tool is for authorized security testing only. Unauthorized use is illegal.%n%nIt is recommended that you close all other applications before continuing.
FinishedLabel=Setup has finished installing [name] on your computer.%n%nYou can now launch ShadowStrike from the Start Menu or Desktop shortcut.%n%n** TIP ** Access comprehensive documentation through the Help menu in the application.

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  if MsgBox('ShadowStrike v2.0 - Security Testing Platform' + #13#10 + #13#10 + 
            '** LEGAL DISCLAIMER **' + #13#10 + #13#10 + 
            'This software includes powerful security testing modules:' + #13#10 +
            '  * OSINT Engine (reconnaissance and intelligence gathering)' + #13#10 +
            '  * DDoS Testing (HTTP/SYN/UDP flood attacks)' + #13#10 +
            '  * Injection Testing (SQL injection and file upload vulnerabilities)' + #13#10 +
            '  * Ransomware Analysis (security testing and detection)' + #13#10 +
            '  * Terminal Interface (command-line access)' + #13#10 + #13#10 +
            'By installing this software, you agree to:' + #13#10 +
            '  [X] Use it ONLY on systems you own or have explicit written permission to test' + #13#10 +
            '  [X] Comply with all applicable local, state, and federal laws' + #13#10 +
            '  [X] Accept full responsibility for your actions' + #13#10 + #13#10 +
            'Unauthorized use may result in criminal prosecution, civil lawsuits, and imprisonment.' + #13#10 + #13#10 +
            'Do you accept these terms and wish to continue?', 
            mbConfirmation, MB_YESNO) = IDNO then
    Result := False;
end;
