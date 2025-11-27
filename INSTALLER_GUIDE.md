# ShadowStrike Installer Guide

## Goal
Create a professional single-click installer for ShadowStrike.

## Step 1: Create Self-Contained Executable
Run this command to build the application with all dependencies included:

```powershell
dotnet publish ShadowStrike.UI/ShadowStrike.UI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

**Result**: A single `ShadowStrike.UI.exe` file in the `publish` folder (~150MB).

## Step 2: Create Installer (Inno Setup)

1.  **Download & Install**: [Inno Setup](https://jrsoftware.org/isdl.php)
2.  **Create Script**: Save the following code as `setup.iss` in your project root:

```iss
[Setup]
AppName=ShadowStrike
AppVersion=2.0
DefaultDirName={autopf}\ShadowStrike
DefaultGroupName=ShadowStrike
OutputDir=installer
OutputBaseFilename=ShadowStrike-Setup-v2.0
Compression=lzma2
SolidCompression=yes
SetupIconFile=ShadowStrike.UI\img\eagle.ico
WizardStyle=modern
PrivilegesRequired=admin

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\ShadowStrike"; Filename: "{app}\ShadowStrike.UI.exe"
Name: "{autodesktop}\ShadowStrike"; Filename: "{app}\ShadowStrike.UI.exe"

[Run]
Filename: "{app}\ShadowStrike.UI.exe"; Description: "Launch ShadowStrike"; Flags: postinstall nowait skipifsilent
```

3.  **Compile**:
    *   Open `setup.iss` with Inno Setup Compiler.
    *   Click **Build > Compile**.

**Result**: An installer file `ShadowStrike-Setup-v2.0.exe` in the `installer` folder.

## Step 3: Distribute
1.  Go to your GitHub Releases page.
2.  Create a new release (e.g., `v2.0.0`).
3.  Upload `ShadowStrike-Setup-v2.0.exe`.

Users can now download and install ShadowStrike with a single click!
